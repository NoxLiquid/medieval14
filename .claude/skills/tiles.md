# Tiles / Floors

## Location
- Prototypes: `Resources/Prototypes/_Respiral/Tiles/floors.yml`
- Textures: `Resources/Textures/_Respiral/Tiles/`
- Locale (names): `Resources/Locale/ru-RU/_respiral/tiles/tiles.ftl` (no en-US file for _respiral tiles yet)

## Wood plank floors
Each wood variant lives in `Textures/_Respiral/Tiles/WoodPlanks/<Variant>/` with loose PNGs
(no `.rsi`) plus an `attributions.yml`. Variants: DeadWood, BirchWood, SwampWood, DarkWood, Burned.

Pattern per variant:
- An abstract base `Medieval<Variant>WoodPlanksBase` holds shared props: `baseTurf: MedievalLattice`,
  `deconstructTools: [ Prying ]`, `footstepSounds` FootstepWood, `barestepSounds` BarestepWood,
  `heatCapacity: 10000`, `edgeSpritePriority`, `edgeSprites` (8 directions), `weather`, `absorbent`.
- Concrete tiles inherit the base and set `id`, `name` (locale key), `sprite`.
- Full variants also set `variants: N` + matching `placementVariants` array and an `itemDrop`.

Sprite roles: base `woodplanks`, `_big` (large), `_cruciform` (parquet), `_stairways` (chess),
each with a `_broken` counterpart for the full variants.

## Burned variant (ported from crystall-punk-14)
- Textures copied from `_CP14/Tiles/WoodPlanks/Burned` → `_Respiral/Tiles/WoodPlanks/Burned`.
- 4 tiles: `MedievalFloorWoodBurned`, `MedievalFloorWoodLargeBurned`, `MedievalFloorWoodParquetBurned`,
  `MedievalFloorWoodChessBurned`, all inheriting `MedievalBurnedWoodPlanksBase`.
- No `variants`/`itemDrop` (single-variant, non-craftable — burnt result only). Locale keys
  `tiles-wood-burned`, `-large-burned`, `-parquet-burned`, `-chess-burned`.
- NOTE: CP14's `burnedTile:` tile field (auto-burn transformation) does NOT exist in Respiral's engine;
  only the sprites/prototypes were ported, not the burning-transformation mechanic.
