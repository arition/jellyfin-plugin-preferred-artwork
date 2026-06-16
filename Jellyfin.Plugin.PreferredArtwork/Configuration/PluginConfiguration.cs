using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.PreferredArtwork.Configuration;

/// <summary>
/// Controls how proxied remote images are filtered by language and dimensions.
/// </summary>
public enum LanguageSelectionMode
{
    /// <summary>
    /// Do not filter by language. Jellyfin may still re-order images by the item's metadata language.
    /// </summary>
    Disabled,

    /// <summary>
    /// Return only the preferred image language, falling back to configured fallback languages.
    /// </summary>
    PreferredOnlyWithFallback,

    /// <summary>
    /// Return all languages with the preferred language first. Jellyfin may still re-order afterwards.
    /// </summary>
    PreferredFirst
}

/// <summary>
/// Controls how images are ranked after filtering.
/// </summary>
public enum ImageSortMode
{
    /// <summary>
    /// Prefer the configured language bucket, then provider score/votes, then resolution.
    /// </summary>
    LanguageThenProviderQuality,

    /// <summary>
    /// Prefer higher resolution, then language, then provider score/votes.
    /// </summary>
    ResolutionThenLanguage,

    /// <summary>
    /// Prefer provider score/votes, then resolution, then language.
    /// </summary>
    ProviderQualityThenResolution
}

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        SourceProviderName = "TheTVDB";
        PreferredImageLanguage = "ja";
        FallbackImageLanguages = "en,";
        LanguageSelectionMode = LanguageSelectionMode.PreferredOnlyWithFallback;
        SortMode = ImageSortMode.LanguageThenProviderQuality;
        MinWidth = 0;
        MinHeight = 0;
        AllowUnknownDimensions = false;
        IncludeDisabledSourceProvider = true;
        AllowAnyLanguageFallback = false;
    }

    /// <summary>
    /// Gets or sets comma-separated existing remote image provider names to proxy, for example "TheTVDB,TheMovieDb".
    /// </summary>
    public string SourceProviderName { get; set; }

    /// <summary>
    /// Gets or sets the preferred artwork language, for example "ja" or "zh".
    /// </summary>
    public string PreferredImageLanguage { get; set; }

    /// <summary>
    /// Gets or sets comma-separated fallback artwork languages. Use an empty entry for no-language images.
    /// </summary>
    public string FallbackImageLanguages { get; set; }

    /// <summary>
    /// Gets or sets how artwork language is filtered.
    /// </summary>
    public LanguageSelectionMode LanguageSelectionMode { get; set; }

    /// <summary>
    /// Gets or sets how filtered images are sorted.
    /// </summary>
    public ImageSortMode SortMode { get; set; }

    /// <summary>
    /// Gets or sets the minimum accepted image width. Zero disables width filtering.
    /// </summary>
    public int MinWidth { get; set; }

    /// <summary>
    /// Gets or sets the minimum accepted image height. Zero disables height filtering.
    /// </summary>
    public int MinHeight { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether images without source dimensions are allowed.
    /// </summary>
    public bool AllowUnknownDimensions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether disabled source providers may still be proxied.
    /// </summary>
    public bool IncludeDisabledSourceProvider { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether any language may be returned after preferred and fallback languages fail.
    /// </summary>
    public bool AllowAnyLanguageFallback { get; set; }
}
