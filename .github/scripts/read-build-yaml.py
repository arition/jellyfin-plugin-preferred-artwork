#!/usr/bin/env python3
"""Read the subset of build.yaml used by the release workflow."""

import argparse
import os
import re
import uuid
from pathlib import Path


TOP_LEVEL_KEY = re.compile(r"^([A-Za-z][A-Za-z0-9_]*):(.*)$")


def parse_scalar(value: str) -> str:
    value = value.strip()
    if len(value) >= 2 and value[0] == value[-1] and value[0] in {"'", '"'}:
        return value[1:-1]
    return value


def parse_build_yaml(path: Path) -> dict[str, str]:
    lines = path.read_text(encoding="utf-8").splitlines()
    result: dict[str, str] = {}
    index = 0

    while index < len(lines):
        line = lines[index]
        match = TOP_LEVEL_KEY.match(line)
        if not match:
            index += 1
            continue

        key, raw_value = match.group(1), match.group(2).strip()
        if raw_value in {">", ">-", "|", "|-"}:
            block_lines: list[str] = []
            index += 1
            while index < len(lines):
                block_line = lines[index]
                if TOP_LEVEL_KEY.match(block_line):
                    break

                block_lines.append(block_line[2:] if block_line.startswith("  ") else block_line)
                index += 1

            result[key] = "\n".join(block_lines).strip()
            continue

        result[key] = parse_scalar(raw_value)
        index += 1

    return result


def write_output(values: dict[str, str]) -> None:
    output_path = os.environ.get("GITHUB_OUTPUT")
    if not output_path:
        for key, value in values.items():
            print(f"{key}={value}")
        return

    with Path(output_path).open("a", encoding="utf-8") as output:
        for key, value in values.items():
            delimiter = f"EOF_{key}_{uuid.uuid4().hex}"
            output.write(f"{key}<<{delimiter}\n{value}\n{delimiter}\n")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("path", nargs="?", default="build.yaml")
    args = parser.parse_args()

    metadata = parse_build_yaml(Path(args.path))
    required = [
        "name",
        "guid",
        "version",
        "targetAbi",
        "owner",
        "overview",
        "description",
        "category",
        "changelog",
    ]
    missing = [key for key in required if not metadata.get(key)]
    if missing:
        raise SystemExit(f"Missing required build.yaml keys: {', '.join(missing)}")

    write_output({key: metadata[key] for key in required})


if __name__ == "__main__":
    main()
