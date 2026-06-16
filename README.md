# Jellyfin Preferred Artwork

Preferred Artwork is a Jellyfin remote image provider plugin that proxies one or more existing providers, such as `TheTVDB` and `TheMovieDb`, then filters and sorts the returned artwork by language and image dimensions.

Use it when you want metadata in one language but artwork in another language, or when the stock provider returns low-resolution artwork.

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

## GitHub Actions release flow

This repository includes two workflows:

1. `Build` runs on pushes and pull requests to `main` or `master`.
2. `Release` runs on tags like `v1.0.0.0` and publishes a GitHub release asset plus a Jellyfin repository manifest on the `gh-pages` branch.

To publish a release:

```bash
git tag v1.0.0.0
git push origin v1.0.0.0
```

After the release workflow completes, add this repository URL in Jellyfin Dashboard -> Plugins -> Repositories:

```text
https://raw.githubusercontent.com/<owner>/<repo>/gh-pages/manifest.json
```

Replace `<owner>/<repo>` with your GitHub repository path. The release workflow requires repository Actions permissions that allow writing contents so it can create releases and push the `gh-pages` branch.
