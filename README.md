# Jellyfin Preferred Artwork

Preferred Artwork is a Jellyfin remote image provider plugin that proxies one or more existing providers, such as `TheTVDB` and `TheMovieDb`, then filters and sorts the returned artwork by language and image dimensions.

Use it when you want metadata in one language but artwork in another language, or when the stock provider returns low-resolution artwork.

## Plugin Repo

<https://arition.github.io/jellyfin-plugin-preferred-artwork/manifest.json>

## How it works

The plugin exposes a new image provider named `Preferred Artwork`. When Jellyfin asks it for remote images, it calls Jellyfin's existing provider manager for each configured source provider, requests all languages, merges the returned `RemoteImageInfo` records, filters them, and returns a ranked list to Jellyfin.

Recommended setup:

1. Install the source provider plugins, for example TheTVDB and TMDb.
2. Install this plugin.
3. Configure `Preferred Artwork` with `SourceProviderName = TheTVDB,TheMovieDb`, `PreferredImageLanguage = ja`, and your minimum width/height.
4. In the library image fetcher order, put `Preferred Artwork` before the source providers, or disable the source provider image fetchers and leave `Proxy disabled source providers` enabled.
5. Refresh metadata/images with image replacement enabled for existing items.

## Build

```bash
dotnet publish Jellyfin.Plugin.PreferredArtwork/Jellyfin.Plugin.PreferredArtwork.csproj -c Release
```

Copy the publish output into a plugin folder under Jellyfin's plugins directory and restart Jellyfin.

