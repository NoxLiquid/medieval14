#!/usr/bin/env python3
"""Split a sprite into per-color-group layers with a color tolerance.

Finds every opaque colour in the input(s), groups them into perceptual families
(e.g. blues, yellows, whites) and writes ONE PNG per group containing only that
group's pixels (original colours, alpha preserved; everything else transparent).

Grouping:
  * chromatic colours (saturation >= --sat-thresh) are single-link chained by circular
    HUE distance <= --hue-tol, so a shaded gradient of one hue stays one layer;
  * neutral colours (low saturation: whites/greys/blacks) are bucketed by lightness.

Colours are collected from ALL inputs together, so multiple states (icon + equipped-*)
split into the SAME named layers.

Usage:
    python split_color_layers.py [options] <input.png | input.rsi_dir> [more...]

Options:
    --out DIR         output directory (default: alongside each input)
    --hue-tol DEG     hue tolerance in degrees for chaining chromatic colours (default 25)
    --sat-thresh S    saturation 0..1 below which a colour is neutral (default 0.18)
    --val-tol V       lightness tolerance 0..1 for chaining neutral (grey/black) tones (default 0.30)
    --min-pixels N    drop groups with fewer than N pixels total (default 1)

Output files:  <stem>_<groupname>.png   (groupname e.g. blue / yellow / white)
"""
import colorsys
import os
import sys
from collections import Counter

try:
    from PIL import Image
except ImportError:
    sys.exit("Pillow is required: pip install Pillow")


def hue_dist(a, b):
    d = abs(a - b) % 360.0
    return min(d, 360.0 - d)


def hue_name(h):
    for lim, name in ((15, "red"), (45, "orange"), (70, "yellow"), (160, "green"),
                      (200, "cyan"), (255, "blue"), (290, "purple"), (345, "pink")):
        if h < lim:
            return name
    return "red"


def neutral_name(v):
    if v >= 0.70:
        return "white"
    if v <= 0.25:
        return "black"
    return "grey"


def iter_pngs(path):
    if os.path.isdir(path):
        for root, _d, files in os.walk(path):
            for f in files:
                if f.lower().endswith(".png"):
                    yield os.path.join(root, f)
    elif path.lower().endswith(".png"):
        yield path


def cluster_chromatic(chroma, hue_tol):
    # chroma: list of (rgb, count, hue_deg). Single-link union-find on hue distance.
    n = len(chroma)
    parent = list(range(n))

    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i

    def union(i, j):
        parent[find(i)] = find(j)

    for i in range(n):
        for j in range(i + 1, n):
            if hue_dist(chroma[i][2], chroma[j][2]) <= hue_tol:
                union(i, j)
    groups = {}
    for i, (rgb, cnt, hue) in enumerate(chroma):
        groups.setdefault(find(i), []).append((rgb, cnt, hue))
    return list(groups.values())


def cluster_neutral(neutral, val_tol):
    # neutral: list of (rgb, count, value). Single-link on value distance, so a shaded
    # grey/black body (many neutral tones) stays ONE layer instead of splitting by band.
    n = len(neutral)
    parent = list(range(n))

    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i

    def union(i, j):
        parent[find(i)] = find(j)

    for i in range(n):
        for j in range(i + 1, n):
            if abs(neutral[i][2] - neutral[j][2]) <= val_tol:
                union(i, j)
    groups = {}
    for i, m in enumerate(neutral):
        groups.setdefault(find(i), []).append(m)
    return list(groups.values())


def build_groups(colors, hue_tol, sat_thresh, val_tol):
    chroma, neutral = [], []
    for (r, g, b), cnt in colors.items():
        h, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
        if s < sat_thresh:
            neutral.append(((r, g, b), cnt, v))
        else:
            chroma.append(((r, g, b), cnt, h * 360.0))

    clusters = []  # (name, set_of_rgb, total_count)
    for members in cluster_chromatic(chroma, hue_tol):
        total = sum(c for _rgb, c, _h in members)
        # representative hue = most frequent member's hue
        rep = max(members, key=lambda m: m[1])
        clusters.append([hue_name(rep[2]), {m[0] for m in members}, total])

    for members in cluster_neutral(neutral, val_tol):
        total = sum(c for _rgb, c, _v in members)
        rep = max(members, key=lambda m: m[1])
        clusters.append([neutral_name(rep[2]), {m[0] for m in members}, total])

    # dedup names (append index on collision), order by count desc
    clusters.sort(key=lambda c: -c[2])
    seen = {}
    for c in clusters:
        base = c[0]
        if base in seen:
            seen[base] += 1
            c[0] = f"{base}{seen[base]}"
        else:
            seen[base] = 1
    return clusters


def main(argv):
    out_dir = None
    hue_tol = 25.0
    sat_thresh = 0.18
    val_tol = 0.30
    min_pixels = 1
    inputs = []
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == "--out":
            i += 1; out_dir = argv[i]
        elif a == "--hue-tol":
            i += 1; hue_tol = float(argv[i])
        elif a == "--sat-thresh":
            i += 1; sat_thresh = float(argv[i])
        elif a == "--val-tol":
            i += 1; val_tol = float(argv[i])
        elif a == "--min-pixels":
            i += 1; min_pixels = int(argv[i])
        elif a in ("-h", "--help"):
            print(__doc__); return 0
        else:
            inputs.append(a)
        i += 1

    pngs = [p for arg in inputs for p in iter_pngs(arg)]
    if not pngs:
        print(__doc__); return 1

    colors = Counter()
    loaded = []
    for path in pngs:
        im = Image.open(path).convert("RGBA")
        loaded.append((path, im))
        for r, g, b, a in im.getdata():
            if a > 0:
                colors[(r, g, b)] += 1

    clusters = [c for c in build_groups(colors, hue_tol, sat_thresh, val_tol) if c[2] >= min_pixels]
    print(f"{len(colors)} unique colours -> {len(clusters)} groups: "
          + ", ".join(f"{name}({total})" for name, _s, total in clusters))

    color_to_group = {}
    for name, rgbs, _t in clusters:
        for rgb in rgbs:
            color_to_group[rgb] = name

    for path, im in loaded:
        stem = os.path.splitext(os.path.basename(path))[0]
        dest = out_dir if out_dir else os.path.dirname(path)
        os.makedirs(dest, exist_ok=True)
        pixels = list(im.getdata())
        for name, _rgbs, _t in clusters:
            data = [(r, g, b, a) if (a > 0 and color_to_group.get((r, g, b)) == name)
                    else (0, 0, 0, 0) for (r, g, b, a) in pixels]
            if not any(px[3] for px in data):
                continue
            layer = Image.new("RGBA", im.size)
            layer.putdata(data)
            outpath = os.path.join(dest, f"{stem}_{name}.png")
            layer.save(outpath)
            print(f"wrote {outpath}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
