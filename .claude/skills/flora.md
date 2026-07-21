# Flora — trees and bushes

Domain: static plant objects (trees, bushes), their prototypes and RSI sprites.

## Where things live

- **Prototypes:** `Resources/Prototypes/_Respiral/Entities/Objects/RawMaterials/Flora/`
  - `base_flora.yml` — the abstract base `MedievalBaseTree`.
  - `tree.yml` — trees (`MedievalTree`, `MedievalTreeLarge`, `MedievalTreeMini*`).
  - `bushs.yml` — bushes (`MedievalFlora*Kust*`).
- **Sprites:** `Resources/Textures/_Respiral/Objects/Flora/*.rsi`.

## Base `MedievalBaseTree` (base_flora.yml)

- `Sprite`: `noRot: true`, `drawdepth: Overdoors`, `offset: 0,0.9` (default for 192×256 sprites).
- Static physics + trunk fixture (`-0.35,-0.4 .. 0.35,0.4`, `WallLayer`).
- `Damageable: MedievalWood`, `Injurable: StructuralInorganic`, chopping with an axe (`chop.ogg`).
- `Destructible` at 500 damage: sound `tree_fell.ogg`, drop `MedievalLog` 1–3.
- `MedievalTreeLarge` overrides the fixture/drop (2–4 logs) and its own `offset 0,1.55`.

## Harvestable flora (`FloraComponent` + `FloraSystem`)

Server-only system: `Content.Server/_Respiral/Flora/`. `FloraComponent` is `[RegisterComponent]`
(NOT networked) — harvesting is server-authoritative, visuals go through `AppearanceComponent`.

- **Harvest:** `InteractHand`/`InteractUsing` → `DoAfter` (`HarvestDuration`) → spawns `HarvestPrototype`
  ×`[MinYield..MaxYield]`. `SpecialTool` (a `ToolQualityPrototype`) gates harvesting if set.
- **Regrowth via `TimedWindow`** (`Content.Shared/_Respiral/TimeWindow/`, see [[mana]]): the component
  holds `GrowthWindow = new TimedWindow(min, max)`. On harvest → `_timedWindow.Reset(GrowthWindow)` picks a
  random delay; `Update` checks `_timedWindow.IsExpired(GrowthWindow)` → regrows. Do NOT hand-roll
  `NextGrowthTick`/`CurTime` math — use `TimedWindowSystem`. YAML: `growthWindow: { min: 10m, max: 20m }`
  (TimeSpan parses suffixes `s`/`m`/`h`, or a bare number = seconds).

### Visuals: `GenericVisualizer`, NOT custom client code

Grown vs harvested sprite is driven entirely by the **engine's** `GenericVisualizerSystem`
(`RobustToolbox/Robust.Client`), no Respiral client C#:

1. Server `FloraSystem.UpdateAppearance` → `_appearance.SetData(uid, FloraVisuals.State, FloraState.X)`.
2. `AppearanceComponent` (networked) replicates → client `AppearanceChangeEvent`.
3. `GenericVisualizerSystem` reads the key, does `value.ToString()` (= enum member name `Grown`/`Harvested`),
   looks it up in the YAML `GenericVisualizer.visuals` map, sets the sprite layer state.

**Gotcha (cost a debugging session 2026-06):** the visual enums `FloraVisuals` + `FloraState` MUST live in
`Content.Shared` and be `[Serializable, NetSerializable]` — they travel in appearance data and resolve on the
CLIENT. Putting them in `Content.Server` (or deleting `FloraVisuals` as "unused, no client code") breaks the
visualizer / fails the YAML linter (`Failed to parse enum enum.FloraVisuals.State`). Prototype needs all of:
`- type: Appearance`, `- type: GenericVisualizer` (keys = `FloraState` member names), and a sprite layer
`map: ["flora"]`.

```yaml
- type: Flora
  harvestPrototype: FoodBerriesTroynika
  growthWindow: { min: 10m, max: 20m }
- type: GenericVisualizer
  visuals:
    enum.FloraVisuals.State:
      flora:
        Grown: { state: big_troynika_b }
        Harvested: { state: big_troynika_h }
```

## RandomSprite convention (several visuals in one prototype)

The standard for trees/bushes: one `Sprite` with a layer `map: ["random"]` + a
`RandomSprite` component that picks a random state. The state names in the RSI are `tree01`, `tree02`, …
192×256 bush sprites use the base's default `offset 0,0.9` (don't override the offset).

```yaml
- type: Sprite
  sprite: _Respiral/Objects/Flora/<name>.rsi
  layers:
  - state: tree01
    map: ["random"]
- type: RandomSprite
  available:
  - random:
      tree01: ""
      tree02: ""
```

## Fantasy trees mini_trees.rsi (2026-06, by mars33_2.0 for Respiral)

The spriter sent the pack in `Textures/_Respiral/ФЭНТЕЗИ!!!/Объекты/Mini.rsi` (Cyrillic + `!!!` in the path,
no `meta.json`, inconsistent PNG names). Integrated into the namespace:
`Resources/Textures/_Respiral/Objects/Flora/mini_trees.rsi`, the old folder deleted.

- 27 PNG, three color families: **green** (10), **black** (8), **white** (9).
- State names normalized to ASCII: `green01..green10`, `black01..black08`, `white01..white09`.
- **Important about RSI size:** all states in one .rsi must share the same size. The original
  green sprites were 192×**224**, black/white — 192×**256**. The green images were padded with transparent
  pixels at the top up to 192×256 (content aligned to the bottom — the trunk stays at the bottom) so the whole
  pack fits into a single RSI of size 192×256.
- `meta.json`: `license: CC-BY-SA-3.0`, `copyright: Created by mars33_2.0 for ... Respiral`.

Prototypes in `tree.yml` (parent `MedievalBaseTree`, one per family, RandomSprite per family):
`MedievalTreeMini` (lush tree, green), `MedievalTreeMiniBlack` (black tree, black),
`MedievalTreeMiniWhite` (white tree, white). No biome/spawner bindings yet.

## Importing a pack from a spriter — checklist

- Move the RSI from the temporary Cyrillic folder (`ФЭНТЕЗИ!!!/…`) to `_Respiral/Objects/Flora/`.
- Create/rewrite `meta.json` (size = actual pixels, states = actual PNGs).
- Normalize state names to ASCII; **check that all PNGs in the RSI share the same size** (pad if
  they differ — otherwise the engine won't load the RSI).
- Delete the original temporary folder after the move.
