#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SRC_DIR="$ROOT_DIR/assets-src"
SRC="$SRC_DIR/ghost-cat-crop.png"
OUT="$ROOT_DIR/assets"

mkdir -p "$OUT"

# pet sprite
magick "$SRC" \
    -background none \
    -resize 128x128 \
    -gravity center \
    -extent 128x128 \
    -alpha set \
    -channel A -evaluate multiply 0.75 \
    "$OUT/pet.png"

# pet icon (multi resolution)
magick "$SRC" \
    -background none \
    -filter Lanczos \
    \( +clone -resize 256x256 -gravity center -extent 256x256 \) \
    \( +clone -resize 128x128 -gravity center -extent 128x128 \) \
    \( +clone -resize 64x64  -gravity center -extent 64x64  \) \
    \( +clone -resize 48x48  -gravity center -extent 48x48  \) \
    \( +clone -resize 32x32  -gravity center -extent 32x32  \) \
    \( +clone -resize 24x24  -gravity center -extent 24x24  \) \
    \( +clone -resize 20x20  -gravity center -extent 20x20  \) \
    \( +clone -resize 16x16  -gravity center -extent 16x16  \) \
    -delete 0 \
    "$OUT/icon.ico"

magick "$SRC" \
    -resize 630x500 \
    -gravity center \
    -background none \
    -extent 630x500 \
    "$SRC_DIR/itch-picture.png"

echo "Assets generated:"
echo "  $OUT/pet.png"
echo "  $OUT/icon.ico"
echo "  $SRC_DIR/itch-picture.png"
