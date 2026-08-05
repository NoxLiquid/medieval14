# MaterialColor — tint sprites/in-hand from a material's color

Single source of truth for material tint: the `color` field on a `type: material` prototype.
Instead of hardcoding `color: "#4B0000"` in every weapon/tool sprite layer and in-hand visual,
an entity carries `MaterialColor` and the client applies the material's color at spawn.

## Why
YAML anchors are single-file only and there is no cross-file constant system in Robust prototypes,
so a color shared between `materials.yml` and entity files (e.g. `Weapon/*.yml`) can't be a YAML
variable. Solved at runtime instead.

## Pieces (client-only)
- `Content.Client/_Respiral/Materials/MaterialColorVisualsComponent.cs`
  - `Layers`: `Dictionary<string, ProtoId<MaterialPrototype>>` — sprite **LayerMap key** → material.
  - `Inhand`: `ProtoId<MaterialPrototype>?` — material whose color paints **all** in-hand layers.
- `Content.Client/_Respiral/Materials/MaterialColorSystem.cs` (client-only, visuals)

### Why the name ends in `Visuals` (important framework rule)
The component is purely client visual, so it lives in `Content.Client`. But the **server also parses
every entity prototype**, so it sees `- type: MaterialColorVisuals` on the knife. Server-side unknown
components are handled by `ComponentFactory.GetComponentAvailability`:
- `Content.Server/Entry/EntryPoint.cs` calls `IgnoreMissingComponents("Visuals")` → an unregistered
  component is silently `Ignore`d **only if its name ends with `Visuals`**; otherwise it's `Unknown`
  and logs `Unknown component '...' in prototype!` for every prototype using it (spam; fails clean-load
  tests).
- `Content.Client/Entry/EntryPoint.cs` calls `IgnoreMissingComponents()` (empty postfix) → the client
  ignores *any* unknown component (that's how server-only components don't break the client).

⇒ **A client-only component referenced in shared prototypes MUST be named `…Visuals`.** Otherwise put
it in `Content.Shared`. (First draft put it in Shared to dodge this; renamed + moved to Client instead.)
  - On `ComponentStartup` (2-pass init guarantees `SpriteComponent` layer map is already built):
    - for each `Layers` entry: `LayerMapTryGet(key, logMissing:false)` then `LayerSetColor(index, material.Color)`.
    - if `Inhand` set: mutate every `ItemComponent.InhandVisuals[*]` layer's `.Color` (PrototypeLayerData
      is a mutable class), then `SharedItemSystem.VisualsChanged` to refresh if already held.

## Prototype usage (example: `MedievalAdamantineKnife`, `Weapon/knife.yml`)
```yaml
- type: Sprite
  layers:
  - state: blade
    map: ["blade"]      # REQUIRED: the layer must be mapped so the dict key resolves
  - state: handle
- type: MaterialColorVisuals
  layers:
    blade: MedievalProcessedAdamantine
  inhand: MedievalProcessedAdamantine
- type: Item
  inhandVisuals:
    left:  [ { state: inhand-left } ]   # no color: — MaterialColor sets it
    right: [ { state: inhand-right } ]
```

## Gotchas
- The targeted sprite layer MUST have a `map:` key; unmapped layers are silently skipped.
- Color is applied once at startup (static). It is NOT networked/appearance-driven — fine because a
  material's color never changes at runtime.
- `Inhand` paints every in-hand layer with one material. For multi-material items where blade and
  handle differ in-hand, this component would need extending to a per-inhand-layer dict.

## Migration status: `Entities/Items/Weapon/*` — DONE (2026-08)
All 78 material-tinted weapon variants across sword/dagger/cleaver/mace/brass_knuckles/whip/
eveningstar/halberd/polearm/spear/hide_dagger/knife now use `MaterialColorVisuals` instead of
hardcoded `color:`. Only 6 distinct colors were ever used in this folder, one-to-one with 6
materials (verified no entity mixed two colors):
```
#8A8A8A → MedievalProcessedIron      #C0C0FF → MedievalProcessedSilver
#8B4513 → MedievalProcessedWood      #9370DB → MedievalProcessedMithril
#B0C4DE → MedievalSteel              #4B0000 → MedievalProcessedAdamantine
```
Map-key convention kept per-file: `eveningstar.yml`/`halberd.yml`/`polearm.yml`/`spear.yml` use
`base`/`handle` (matches their two-handed base prototypes' existing layer naming); everything else
uses `blade`/`handle`. The `handle` layer is only in `Layers` when it was independently colored
(the "Wood" variant of each weapon type colors handle to match blade; metal variants leave the
handle uncolored/default).

**Not migrated, left as-is:**
- `base.yml` — abstract parent prototypes still carry a default `color: "#8A8A8A"` on their `blade`
  layer, but it's dead: every concrete weapon fully redefines its own `Sprite.layers` (lists replace,
  not merge, on inheritance — see [[skills]] Prototype Inheritance Rules), so the base color is never
  rendered. Left untouched rather than cleaned up, to keep this migration YAML-only and low-risk.
- `crafted.yml`, `magic.yml`, `MedievalPeltWhip`/`MedievalPeltWhip2` in `whip.yml` — never had a
  `color:` override (stone-axe, magic swords, leather whips use their raw RSI art or a pelt tone
  with no hex hardcode), so there was nothing to migrate.

## Migration status: `Entities/Objects/Drinks/drinks.yml` + `Entities/Items/Light/candle.yml` — DONE (2026-08)
19 more entities: metal beer mugs (`icon-0` layer — kept alongside its existing
`enum.SolutionContainerLayers.Base` map key; a layer can carry both an enum key, used by
`SolutionContainerVisualsSystem` for fill levels, and a plain string key, used by `MaterialColorVisuals`
— see e.g. `Mobs/Flora/*/*.yml`'s `map: ["enum.DamageStateVisualLayers.Base", "movement"]` for existing
precedent of mixed key types on one layer), wood-bound mugs (`metall` layer), goblets (`iron_goblet`
layer — only Steel/Silver variants were colored; Iron/Gold goblets use distinct uncolored RSI states
`iron_goblet`/`gold_goblet` art, nothing to migrate there), and candelabras 1/3/mini
(`candelabra_metal_1`/`_3`/`_mini` layer + inhand). Added `#FFD700 → MedievalProcessedGold` to the
hex↔material table for this batch (gold wasn't used in the Weapon folder).

**Explicitly NOT migrated (found during the sweep, left hardcoded per user decision, 2026-08):** these
need a *different* mechanism than `MaterialColorVisuals` currently supports — the whole `Sprite.color`
root field, not a named layer — and weren't in scope:
- `Entities/Objects/ProcessedMaterials/production.yml` — `MedievalPartRodWood` (root `Sprite.color`, no `layers:` at all).
- `Entities/Structures/Machine/frame.yml` — `MedievalUnfinishedMachineFrame`/`MedievalMachineFrame` (root color).
- `Entities/Structures/Walls/railing.yml` — `MedievalBaseRailingWood`/`MedievalBaseRailingStone` (root
  color set on the *abstract parent*, inherited into every concrete railing piece — not dead code, unlike
  `Weapon/base.yml`, since these concrete children never redefine `Sprite.color`).
- `ZLevels/stairs.yml` — ladder wood/stone (root color); also MIT-licensed CrystallEdge port, extra
  reason to leave alone without explicit sign-off (see CLAUDE.md ZLevels licensing note).
- `Entities/Objects/Bureaucracy/paper.yml` — `MedievalPaper`/`MedievalPaperWritten` (`paper`/`paper_words`
  layers hardcode `#bbb19e`, which is literally the color chosen for the `MedievalPaper` material —
  see [[bureaucracy]]).

If any of these get tackled later, `MaterialColorVisualsComponent` needs a new field (e.g. `RootColor:
ProtoId<MaterialPrototype>?`) applied via `SpriteSystem.SetColor(sprite, material.Color)` instead of
`LayerSetColor` — no `map:`/layer-key plumbing needed for that path.

**Not migrated, false positives (different subsystem, coincidental color reuse — correctly left alone):**
- `Roles/Jobs/departments.yml` — `#C0C0C0`/`#8B4513` are **department** UI colors (chat/tabs), unrelated
  to materials; hue overlap is coincidental.
- `paper.yml` → `MedievalStampBank.stampedColor: "#8B4513"` — a `Stamp` component field (ink color in
  the paper UI), not a `Sprite`; not tied to a material semantically.
- `Reagents/chemicals.yml` (`MedievalMana`) + `RawMaterials/production.yml`
  (`MedievalManaCrystal`'s `PointLight.color`) both use `#47f8ff` — `Reagent.color` and `PointLight.color`
  are different rendering paths from `Sprite` layers; conflating reagent/light/material color into one
  system would be a separate design decision, not a mechanical migration.
- `test.yml` (`MedievalTestSword` etc.) — appears to be scratch/debug content, not real production entities.
