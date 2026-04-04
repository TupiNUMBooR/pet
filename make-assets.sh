#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 1 ]]; then
    echo "usage: $0 source-image"
    exit 1
fi

SRC="$1"
OUT="assets"

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

echo "Assets generated:"
echo "  $OUT/pet.png"
echo "  $OUT/icon.ico"
