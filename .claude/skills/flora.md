# Flora — trees and bushes

Domain: static plant objects (trees, bushes), their prototypes and RSI sprites.

## Where things live

- **Prototypes:** `Resources/Prototypes/_Respiral/Entities/Objects/RawMaterials/Flora/`
  - `base_flora.yml` — the abstract base `MedievalBaseTree`.
  - `tree.yml` — trees (`MedievalTree`, `MedievalTreeLarge`, plus per-species prototypes `MedievalTreeFir1..6`,
    `MedievalTreeBirch1..6`, `MedievalTreeSwamp1..5`, `MedievalTreeOak1..8`, each paired with a
    `MedievalTreeDropXToY` abstract mixin that scales log yield, and a `MedievalTree<Species>RandomSpawner`
    marker used from `biome_templates.yml`).
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

## Fantasy trees mini_trees.rsi — REMOVED (2026-07, upstream `new_trees` merge)

Was: 27 PNG, three color families (green/black/white, by mars33_2.0), one `RandomSprite` prototype per
family (`MedievalTreeMini`, `MedievalTreeMiniBlack`, `MedievalTreeMiniWhite`), no biome/spawner bindings.

Superseded by the `flora_trees_{fir,birch,swamp,oak}.rsi` sprite packs + the per-species discrete
prototypes in `tree.yml` (see above), which upstream wired into `biome_templates.yml` via the
`RandomSpawner` markers. When merging `upstream/master` into `evaporation` (2026-07-31), `tree.yml` was
taken entirely from upstream (this new architecture), and the now-unreferenced
`Resources/Textures/_Respiral/Objects/Flora/mini_trees.rsi` folder was deleted as orphaned — nothing else
in the repo referenced it.

## Importing a pack from a spriter — checklist

- Move the RSI from the temporary Cyrillic folder (`ФЭНТЕЗИ!!!/…`) to `_Respiral/Objects/Flora/`.
- Create/rewrite `meta.json` (size = actual pixels, states = actual PNGs).
- Normalize state names to ASCII; **check that all PNGs in the RSI share the same size** (pad if
  they differ — otherwise the engine won't load the RSI).
- Delete the original temporary folder after the move.
