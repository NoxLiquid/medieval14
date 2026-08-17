#!/usr/bin/env python3
"""Convert RSI/PNG sprites to grayscale in place, preserving the alpha channel.

This is used to prepare imported colored sprites for the Respiral "grayscale + per-layer
tint" convention (see .claude/skills/clothing.md), so a single sprite can be recolored per
prototype via a `color:` multiply.

Usage:
    python grayscale_rsi.py [--normalize] [--lo N] [--hi N] <path> [<path> ...]

Each <path> may be:
  * a single .png file, or
  * a directory (e.g. an .rsi folder) — every *.png under it is converted recursively.

Grayscale uses Rec.709 luma (0.2126 R + 0.7152 G + 0.0722 B). Alpha is left untouched.
Files are overwritten in place, so copy the sprites into the repo first.

Options:
    --normalize   histogram-stretch the luma of the opaque pixels to [--lo, --hi], POOLED
                  across all inputs (so multiple states/layers keep a consistent mapping).
                  Use this when a dark source sprite comes out too dark to tint (multiply
                  only darkens) — brightening the grayscale lets `color:` read brighter.
    --lo N        low end of the normalized range (default 45)
    --hi N        high end of the normalized range (default 235)
"""
import os
import sys

try:
    from PIL import Image
except ImportError:
    sys.exit("Pillow is required: pip install Pillow")


def luma(r, g, b):
    return 0.2126 * r + 0.7152 * g + 0.0722 * b


def iter_pngs(path):
    if os.path.isdir(path):
        for root, _dirs, files in os.walk(path):
            for f in files:
                if f.lower().endswith(".png"):
                    yield os.path.join(root, f)
    elif path.lower().endswith(".png"):
        yield path
    else:
        print(f"skip (not a .png or directory): {path}")


def main(argv):
    normalize = False
    lo, hi = 45.0, 235.0
    inputs = []
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == "--normalize":
            normalize = True
        elif a == "--lo":
            i += 1; lo = float(argv[i])
        elif a == "--hi":
            i += 1; hi = float(argv[i])
        elif a in ("-h", "--help"):
            print(__doc__); return 0
        else:
            inputs.append(a)
        i += 1

    pngs = [p for arg in inputs for p in iter_pngs(arg)]
    if not pngs:
        print(__doc__)
        return 1

    images = [(p, Image.open(p).convert("RGBA")) for p in pngs]

    gmin, gmax = 255.0, 0.0
    if normalize:
        for _p, im in images:
            for r, g, b, a in im.getdata():
                if a > 0:
                    lm = luma(r, g, b)
                    gmin = min(gmin, lm)
                    gmax = max(gmax, lm)
        span = max(1.0, gmax - gmin)

    def mapval(lm):
        if not normalize:
            return round(lm)
        return round(lo + (lm - gmin) * (hi - lo) / span)

    for p, im in images:
        px = im.load()
        w, h = im.size
        for y in range(h):
            for x in range(w):
                r, g, b, a = px[x, y]
                if a == 0:
                    continue
                v = max(0, min(255, mapval(luma(r, g, b))))
                px[x, y] = (v, v, v, a)
        im.save(p)
        print(f"grayscaled{'+normalized' if normalize else ''}: {p}")
    if normalize:
        print(f"pooled luma range [{gmin:.0f},{gmax:.0f}] -> [{lo:.0f},{hi:.0f}]")
    print(f"done, {len(images)} file(s)")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
