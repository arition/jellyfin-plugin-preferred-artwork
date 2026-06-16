using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.PreferredArtwork.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.PreferredArtwork.Providers;

/// <summary>
/// Proxies an existing remote image provider and applies language and resolution preferences.
/// </summary>
public sealed class PreferredArtworkImageProvider : IRemoteImageProvider, IHasOrder
{
    private static readonly HttpClient HttpClient = new();
    private readonly IProviderManager _providerManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PreferredArtworkImageProvider"/> class.
    /// </summary>
    /// <param name="providerManager">Provider manager used to query the source provider.</param>
    public PreferredArtworkImageProvider(IProviderManager providerManager)
    {
        _providerManager = providerManager;
    }

    /// <inheritdoc />
    public string Name => Plugin.ProviderName;

    /// <inheritdoc />
    public int Order => 10;

    /// <inheritdoc />
    public bool Supports(BaseItem item)
    {
        return item.SupportsRemoteImageDownloading;
    }

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        foreach (var imageType in Enum.GetValues<ImageType>())
        {
            yield return imageType;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        var configuration = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var sourceProviderNames = ParseProviderNames(configuration.SourceProviderName).ToArray();
        if (sourceProviderNames.Length == 0)
        {
            return [];
        }

        var sourceImageTasks = sourceProviderNames
            .Select(providerName => _providerManager.GetAvailableRemoteImages(
                item,
                new RemoteImageQuery(providerName)
                {
                    IncludeAllLanguages = true,
                    IncludeDisabledProviders = configuration.IncludeDisabledSourceProvider
                },
                cancellationToken));

        var sourceImageGroups = await Task.WhenAll(sourceImageTasks).ConfigureAwait(false);
        var sourceImages = sourceImageGroups.SelectMany(images => images);

        var dimensionFiltered = sourceImages
            .Where(image => MeetsDimensionFilter(image, configuration))
            .ToList();

        var languageFiltered = ApplyLanguageSelection(dimensionFiltered, configuration);
        return SortImages(languageFiltered, configuration).ToList();
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return HttpClient.GetAsync(new Uri(url), cancellationToken);
    }

    private static bool MeetsDimensionFilter(RemoteImageInfo image, PluginConfiguration configuration)
    {
        if (configuration.MinWidth <= 0 && configuration.MinHeight <= 0)
        {
            return true;
        }

        if (configuration.MinWidth > 0 && image.Width is null)
        {
            return configuration.AllowUnknownDimensions;
        }

        if (configuration.MinHeight > 0 && image.Height is null)
        {
            return configuration.AllowUnknownDimensions;
        }

        return (configuration.MinWidth <= 0 || image.Width >= configuration.MinWidth) &&
            (configuration.MinHeight <= 0 || image.Height >= configuration.MinHeight);
    }

    private static IEnumerable<RemoteImageInfo> ApplyLanguageSelection(
        IReadOnlyList<RemoteImageInfo> images,
        PluginConfiguration configuration)
    {
        if (configuration.LanguageSelectionMode == LanguageSelectionMode.Disabled)
        {
            return images;
        }

        if (configuration.LanguageSelectionMode == LanguageSelectionMode.PreferredFirst)
        {
            return images;
        }

        return images
            .GroupBy(image => image.Type)
            .SelectMany(group => ApplyLanguageSelectionForType(group.ToList(), configuration));
    }

    private static IEnumerable<RemoteImageInfo> ApplyLanguageSelectionForType(
        IReadOnlyList<RemoteImageInfo> images,
        PluginConfiguration configuration)
    {
        var preferred = images
            .Where(image => IsLanguageMatch(image.Language, configuration.PreferredImageLanguage))
            .ToList();

        if (preferred.Count > 0)
        {
            return preferred;
        }

        foreach (var fallbackLanguage in ParseLanguages(configuration.FallbackImageLanguages))
        {
            var fallback = images
                .Where(image => IsLanguageMatch(image.Language, fallbackLanguage))
                .ToList();

            if (fallback.Count > 0)
            {
                return fallback;
            }
        }

        return configuration.AllowAnyLanguageFallback ? images : [];
    }

    private static IOrderedEnumerable<RemoteImageInfo> SortImages(
        IEnumerable<RemoteImageInfo> images,
        PluginConfiguration configuration)
    {
        return configuration.SortMode switch
        {
            ImageSortMode.ResolutionThenLanguage => images
                .OrderByDescending(GetPixelCount)
                .ThenByDescending(image => GetLanguageScore(image.Language, configuration))
                .ThenByDescending(GetProviderQuality)
                .ThenByDescending(image => image.VoteCount ?? 0),

            ImageSortMode.ProviderQualityThenResolution => images
                .OrderByDescending(GetProviderQuality)
                .ThenByDescending(image => image.VoteCount ?? 0)
                .ThenByDescending(GetPixelCount)
                .ThenByDescending(image => GetLanguageScore(image.Language, configuration)),

            _ => images
                .OrderByDescending(image => GetLanguageScore(image.Language, configuration))
                .ThenByDescending(GetProviderQuality)
                .ThenByDescending(image => image.VoteCount ?? 0)
                .ThenByDescending(GetPixelCount)
        };
    }

    private static int GetLanguageScore(string? language, PluginConfiguration configuration)
    {
        if (configuration.LanguageSelectionMode == LanguageSelectionMode.Disabled)
        {
            return 0;
        }

        if (IsLanguageMatch(language, configuration.PreferredImageLanguage))
        {
            return 100;
        }

        var fallbackScore = 99;
        foreach (var fallbackLanguage in ParseLanguages(configuration.FallbackImageLanguages))
        {
            if (IsLanguageMatch(language, fallbackLanguage))
            {
                return fallbackScore;
            }

            fallbackScore--;
        }

        return 0;
    }

    private static double GetProviderQuality(RemoteImageInfo image)
    {
        return Math.Round(image.CommunityRating ?? 0, 1, MidpointRounding.AwayFromZero);
    }

    private static long GetPixelCount(RemoteImageInfo image)
    {
        return Convert.ToInt64(image.Width ?? 0, CultureInfo.InvariantCulture) *
            Convert.ToInt64(image.Height ?? 0, CultureInfo.InvariantCulture);
    }

    private static bool IsLanguageMatch(string? actualLanguage, string configuredLanguage)
    {
        if (string.IsNullOrWhiteSpace(configuredLanguage))
        {
            return string.IsNullOrWhiteSpace(actualLanguage);
        }

        return string.Equals(
            NormalizeLanguage(actualLanguage),
            NormalizeLanguage(configuredLanguage),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeLanguage(string? language)
    {
        return language?.Trim().Replace('_', '-').ToLowerInvariant() ?? string.Empty;
    }

    private static IEnumerable<string> ParseLanguages(string? languages)
    {
        if (languages is null)
        {
            yield break;
        }

        foreach (var language in languages.Split(','))
        {
            yield return language.Trim();
        }
    }

    private IEnumerable<string> ParseProviderNames(string? providerNames)
    {
        if (providerNames is null)
        {
            yield break;
        }

        var seenProviderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var providerName in providerNames.Split(','))
        {
            var trimmedProviderName = providerName.Trim();
            if (string.IsNullOrWhiteSpace(trimmedProviderName) ||
                string.Equals(trimmedProviderName, Name, StringComparison.OrdinalIgnoreCase) ||
                !seenProviderNames.Add(trimmedProviderName))
            {
                continue;
            }

            yield return trimmedProviderName;
        }
    }
}
