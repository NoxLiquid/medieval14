#!/usr/bin/env python3
"""
Сборка 64x64 изображений из 32x32 тайлов (игнорируя пустые).

Поведение:
- Обработка идёт построчно.
- ВСЕ пустые тайлы пропускаются.
- Оставшиеся тайлы собираются по 4 подряд:
    [0][1]
    [2][3]
- Без перекрытия.

Использование:
    python split_image_tiles.py input.png output_dir
"""

from __future__ import annotations
import argparse
import os
from PIL import Image


TILE = 32
OUT_SIZE = 64


def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Skip empty tiles completely")
    p.add_argument("input", help="Path to input PNG file")
    p.add_argument("output", help="Path to output directory")
    return p.parse_args()


def is_tile_empty(tile: Image.Image) -> bool:
    bands = tile.getbands()

    if "A" in bands:
        alpha = tile.split()[bands.index("A")]
        return alpha.getbbox() is None

    extrema = tile.getextrema()
    try:
        return all(e[0] == e[1] for e in extrema)
    except Exception:
        return False


def split_image_groups(path_in: str, out_dir: str) -> int:
    img = Image.open(path_in).convert("RGBA")
    w, h = img.size

    cols = w // TILE
    rows = h // TILE

    os.makedirs(out_dir, exist_ok=True)

    def get_tile(cx: int, cy: int) -> Image.Image:
        left = cx * TILE
        top = cy * TILE
        return img.crop((left, top, left + TILE, top + TILE))

    saved = 0

    for ry in range(rows):
        # 🔥 собираем только НЕпустые тайлы строки
        row_tiles = []

        for gx in range(cols):
            tile = get_tile(gx, ry)
            if not is_tile_empty(tile):
                row_tiles.append(tile)

        # теперь режем по 4
        for i in range(0, len(row_tiles), 4):
            if i + 3 >= len(row_tiles):
                break

            t0, t1, t2, t3 = row_tiles[i:i+4]

            out_img = Image.new("RGBA", (OUT_SIZE, OUT_SIZE), (0, 0, 0, 0))

            out_img.paste(t0, (0, 0))
            out_img.paste(t1, (TILE, 0))
            out_img.paste(t2, (0, TILE))
            out_img.paste(t3, (TILE, TILE))

            saved += 1
            out_path = os.path.join(out_dir, f"{saved}.png")
            out_img.save(out_path)

    return saved


def main() -> None:
    args = parse_args()
    count = split_image_groups(args.input, args.output)
    print(f"Saved {count} images to '{args.output}'")


if __name__ == "__main__":
    main()