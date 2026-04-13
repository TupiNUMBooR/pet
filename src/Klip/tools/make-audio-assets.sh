#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SRC_DIR="$ROOT_DIR/assets-src"
OUT="$ROOT_DIR/assets"

mkdir -p "$OUT"

VOLUME="0.2"

echo "[sound] processing..."

for f in "$SRC_DIR"/*.wav; do
  name="$(basename "$f")"

  ffmpeg -loglevel error -y \
    -i "$f" \
    -filter:a "volume=${VOLUME}" \
    "$OUT/$name"
done

echo "[sound] done ✨"
