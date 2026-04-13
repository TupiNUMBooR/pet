#!/usr/bin/env bash
set -euo pipefail
shopt -s nullglob

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SRC_DIR="$ROOT_DIR/assets-src"
OUT="$ROOT_DIR/assets"

mkdir -p "$OUT"

VOLUME="0.2"

echo "[sound] processing..."

for f in "$SRC_DIR"/*.wav "$SRC_DIR"/*.mp3; do
  name="$(basename "$f")"
  name="${name%.*}.ogg"

  ffmpeg -loglevel error -y \
    -i "$f" \
    -filter:a "volume=${VOLUME}" \
    "$OUT/$name"
done

echo "[sound] done ✨"
