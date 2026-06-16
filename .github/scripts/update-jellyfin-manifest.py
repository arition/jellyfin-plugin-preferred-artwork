#!/usr/bin/env python3
"""Update a Jellyfin plugin repository manifest with one plugin version."""

import argparse
import datetime as dt
import json
from pathlib import Path


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--manifest", required=True)
    parser.add_argument("--guid", required=True)
    parser.add_argument("--name", required=True)
    parser.add_argument("--owner", required=True)
    parser.add_argument("--category", required=True)
    parser.add_argument("--overview", required=True)
    parser.add_argument("--description", required=True)
    parser.add_argument("--version", required=True)
    parser.add_argument("--target-abi", required=True)
    parser.add_argument("--source-url", required=True)
    parser.add_argument("--checksum", required=True)
    parser.add_argument("--changelog", required=True)
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    manifest_path = Path(args.manifest)
    catalog = json.loads(manifest_path.read_text(encoding="utf-8"))

    plugin = next(
        (entry for entry in catalog if entry.get("guid", "").lower() == args.guid.lower()),
        None,
    )
    if plugin is None:
        plugin = {
            "guid": args.guid,
            "name": args.name,
            "description": args.description,
            "overview": args.overview,
            "owner": args.owner,
            "category": args.category,
            "versions": [],
        }
        catalog.append(plugin)

    plugin.update(
        {
            "name": args.name,
            "description": args.description,
            "overview": args.overview,
            "owner": args.owner,
            "category": args.category,
        }
    )

    versions = [
        version
        for version in plugin.get("versions", [])
        if version.get("version") != args.version
    ]
    versions.insert(
        0,
        {
            "version": args.version,
            "changelog": args.changelog,
            "targetAbi": args.target_abi,
            "sourceUrl": args.source_url,
            "checksum": args.checksum,
            "timestamp": dt.datetime.now(dt.UTC).strftime("%Y-%m-%dT%H:%M:%S.0000000Z"),
        },
    )
    plugin["versions"] = versions

    manifest_path.write_text(
        json.dumps(catalog, indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
