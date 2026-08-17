# Wallpaper (обои)

Directional decorative coverings applied to walls. Sprites ported from crystall-punk-14,
re-implemented the Respiral way (no C#) via the **construction system** (like curtains).

## Two-step flow
1. **Craft a roll** at the sewing machine (`MedievalSewing` lathe pack) — `latheRecipe`
   parented to `MedievalBaseWallpaperRecipe` → `MedievalBaseTailorClothingRecipe`,
   `MedievalCloth: 100`, `MedievalTailor: Mastery`. Result = `MedievalWallpaperItem<Color>`
   (a rolled item, tagged `MedievalWallpaper<Color>`).
2. **Paste it** via the **construction menu**: a rotatable ghost (R picks the wall side) whose
   graph step consumes the matching roll (by `tag`). Deconstruct (Cutting tool) returns the roll.

## Why construction (not carpet-style)
Carpets use `SpawnAfterInteract` and have no direction. Wallpaper needs to pick a wall side,
so it's a **construction ghost** the player rotates before building — the sprite's directional
frames then show the correct side.

## Sprite trick (directional without C#)
`wallpaper_<color>.rsi` has 4 states `bottom/top/left/right`, each 32x64 with 4 directions.
Inspection showed each state's content lives ONLY in its matching direction frame
(bottom→South, top→North, right→East, left→West). So the structure Sprite layers all four
states and is **rotatable** (NO `noRot`): whichever way the entity faces, only the one matching
layer/frame has content → exactly one wallpaper side is visible per facing.

## Structure requirements
- `drawdepth: Walls` (same layer as walls) + `SpriteFade` (fades when a mob is behind).
- `Physics canCollide: false` (coexists on the wall tile; recipe has `canBuildInImpassable: true`).
- `- type: Construction { graph: MedievalWallpaper, node: <color> }`.

## Files
- Roll items: `Prototypes/_Respiral/Entities/Items/Misk/wallpaper.yml` (`MedievalWallpaperItem*`, tagged)
- Lathe recipes: `Prototypes/_Respiral/Recipe/wallpaper.yml`, registered in
  `Prototypes/_Respiral/Recipe/packs.yml` under `MedievalSewing`.
- Tags: `Prototypes/_Respiral/tags.yml` (`MedievalWallpaper<Color>`)
- Structures: `Prototypes/_Respiral/Entities/Structures/Decoration/wallpaper.yml` (`MedievalWallpaper*`)
- Construction graph: `Prototypes/_Respiral/Construction/Graph/Structure/wallpaper.yml` (`MedievalWallpaper`,
  edge steps use `tag:` to consume the roll; deconstruct gives the roll back)
- Construction recipes: `Prototypes/_Respiral/Construction/wallpaper.yml`
  (category `construction-category-structures`, `canBuildInImpassable: true`, `icon` from icons.rsi)
- Sprites: `Textures/_Respiral/Structures/Wallpaper/` (icons.rsi + wallpaper_<color>.rsi)
- Names/descriptions are inline Russian (no locale keys), like curtains.

## Variants
9: red, black, green, purple, white, white2, yellow, bank, bank2.

## Note
CP14's stack-based `CP14Wallpaper*` layer/holder C# system was intentionally NOT ported.
