# Construction System

## ConstructionComponent Requirements

`Content.Server.Construction.Components.ConstructionComponent` has `required: true` for both `graph` and `node` fields. Every entity that appears as a node entity in a construction graph MUST have:

```yaml
- type: Construction
  graph: <constructionGraphId>
  node: <nodeName>
```

Without both fields, the entity fails to deserialize (silently skipped — `protoMan.TryIndex` returns false), breaking `ConstructionPrototypeTest`.

## File Layout

| Path | Purpose |
|---|---|
| `Resources/Prototypes/_Respiral/Construction/*.yml` | `construction` recipes (UI entries, link graph→node) |
| `Resources/Prototypes/_Respiral/Construction/Graph/Structure/*.yml` | `constructionGraph` definitions (nodes + edges + steps) |
| `Resources/Prototypes/_Respiral/Entities/Structures/**/*.yml` | Entity prototypes (must have `Construction` component pointing to graph+node) |

## Construction Recipe Pattern

```yaml
- type: construction
  id: MedievalFoo
  graph: MedievalFooGraph      # constructionGraph id
  startNode: start
  targetNode: fooNode           # target node in that graph
  category: construction-category-structures   # or construction-category-furniture
  objectType: Structure
  placementMode: SnapgridCenter
  canRotate: false
  canBuildInImpassable: false
  conditions:
    - !type:TileNotBlocked
```

## Construction Graph Pattern

Two variants:

### Direct (no frame intermediate — fences, roofs, simple furniture)
```yaml
- type: constructionGraph
  id: MedievalFoo
  start: start
  graph:
  - node: start
    edges:
    - to: result
      completed:
        - !type:SnapToGrid
          southRotation: true
      steps:
        - material: MedievalBoard
          amount: 2
          doAfter: 2
  - node: result
    entity: MedievalFooEntity
```

### With Frame intermediate (walls, doors — heavier structures)
```yaml
- node: start → wallFrame (place material) → wall (add more material)
```
Frame entity must exist separately as a girder/frame prototype.

## Materials Used in Construction

| ID | Use |
|---|---|
| `MedievalBoard` | Wood planks (from MedievalProcessedWood) |
| `MedievalProcessedStone` | Cut stone blocks |
| `MedievalConstructionToolsAmmo` | Nails/pegs — required as first step for most structures |
| `MedievalCloth` | Fabric (beds, curtains) |
| `MedievalProcessedPelt` | Tanned leather |

## Destruction Balance (Destructible thresholds)

Damage container: `MedievalInorganic` for all structures.

| Structure type | Modifier set | Destroy HP |
|---|---|---|
| Furniture (small) | MedievalWood | 300 |
| Roof (wooden) | MedievalWood | 600 |
| Roof (birch) | MedievalWood | 700 |
| Roof (stone) | MedievalStone | 1200 |
| Wall (wooden) | MedievalWood | 2000 (crumbles to frame at 1000) |
| Wall (birch) | MedievalWood | 2100 (crumbles at 1050) |

Sound collections on destroy:
- `WoodDestroy` — light structures (fences, small furniture)
- `WoodDestroyHeavy` — heavy structures (walls, doors, roofs)
- No stone-specific sound collection exists yet; use `WoodDestroyHeavy` for stone structures.

## Key/Lock System

**Components:**
- `MedievalKeyComponent.KeyId` — key identifier string
- `MedievalLockComponent.KeyId` — required key; `Locked` — whether the lock is locked
- `MedievalLockableComponent` — on a door/chest: allows locking
- The lock is found via `lock-container` (`ContainerSlot`) on the door, OR via `MedievalLockComponent` directly on the door

**Prototype files:**
| File | Contents |
|---|---|
| `Entities/Items/Misk/key.yml` | Base prototypes (`MedievalBaseKey`, `MedievalKey`, lockpicks) |
| `Entities/Items/Misk/key_sets.yml` | 37 keys by keyId |
| `Entities/Items/Misk/lock.yml` | Base lock prototypes |
| `Entities/Items/Misk/lock_sets.yml` | 37 locks by keyId (`locked: true`) |
| `Entities/Structures/Doors/locked_door.yml` | 222 doors = 6 types × 37 keyId |

**Naming:** `MedievalKey{Latin}` / `MedievalLock{Latin}` / `MedievalWoodenDoor{Latin}`, suffix = Cyrillic keyId.

**keyId list:** blacksmith, tailor, alchemist, carpenter, mason, merchant, tavern, tavern1–6, lord, barracks, prison, elder, granary, warehouse, treasury, guard, house1–10, steward, knight1–5.

**Locked door:** inherits the door type + `ContainerContainer` with an empty `lock-container: !type:ContainerSlot`. The lock (`MedievalLockKuznec` etc.) is placed in the slot on the map, NOT via prototype (ContainerSlot.ent is an EntityUid, not a proto ID). The component name in YAML: `ContainerContainer` (because `[ComponentProtoName("ContainerContainer")]`). `SharedMedievalLockSystem` only accesses the lock inside the container via `GetLock()`; `MedievalLock` on the door is not needed.

## Stairs (ZLevel ladders)

Entities in `Resources/Prototypes/_Respiral/ZLevels/stairs.yml`.
Crafting graph: `MedievalLadder` (`Construction/Graph/Structure/stairs.yml`).
Recipes: `Construction/other.yml`.

| ID | Sprite state | color | Crafting material |
|---|---|---|---|
| `MedievalCEZLevelLadder` | `stairs_wood` | — (no color) | MedievalBoard ×2 |
| `MedievalCEZLevelLadderStone` | `stairs_stone` | `#808080` | MedievalProcessedStone ×2 |
| `MedievalCEZLevelLadderMarble` | `stairs_stone` | `#E8E0C8` | MedievalProcessedStone ×3 |

- Neutral-colored sprites — the material color is set via `color` in the `Sprite` component.
- Marble uses the same `stairs_stone` sprite as stone, but with the ivory color `#E8E0C8`.
- Marble is more expensive to build (×3 stone) and to dismantle.
- Colors come from `materials.yml`: `MedievalProcessedStone → #808080`.

## Roof System

### Entities (`Resources/Prototypes/_Respiral/Entities/Structures/Roof/roof.yml`)

| ID | Sprite RSI | Material |
|---|---|---|
| `MedievalRoofWooden` | `_Respiral/Structures/Roof/wooden.rsi` | MedievalBoard |
| `MedievalRoofBirch` | `_Respiral/Structures/Roof/birch.rsi` | MedievalBoard |
| `MedievalRoofStone` | `_Respiral/Structures/Roof/stone.rsi` | MedievalProcessedStone |

All inherit `MedievalBaseRoof` (abstract). Base uses `test.rsi` as fallback placeholder.

RSI sprite states: `roof0`–`roof7` (4 directions each) + `full` (icon).
`IconSmooth` key: `MedievalRoof`, base: `roof`.

Construction graph: `MedievalRoof` (`Construction/Graph/Structure/roof.yml`).
Construction recipes: `Construction/roof.yml`.

### Roof RCD ("roofing hammer", 2026-08)

`MedievalRoofHammer` (`Entities/Items/Tools/tool.yml`, sprite `hammer.rsi` state `hammer1`, distinct from
`MedievalHammer`/`hammer2` which is the plain melee construction hammer) is a stock SS14 `RCD` item
(`Content.Shared/RCD`) scoped down to roofs only:

- `RCDComponent.availablePrototypes` lists only roof-specific `rcd` prototypes
  (`Resources/Prototypes/_Respiral/RCD/roof.yml`): `MedievalConstructRoofWooden/Birch/Stone`
  (`ConstructObject`, `category: Main` — **must** be `Main`, any other category string not in the
  hardcoded `PrototypesGroupingInfo` dict in `Content.Client/RCD/RCDMenuBoundUserInterface.cs` silently
  drops the entry from the radial menu) and `MedievalDeconstructRoof` (`Deconstruct`).
- Roofs (`MedievalBaseRoof`) got `- type: RCDDeconstructable` + `Tag: [MedievalRoof]`.
- **The stock `RCDSystem.IsDeconstructionStillValid` only checks "does the target have
  `RCDDeconstructableComponent`", not which RCD tool/prototype is doing the deconstructing** — any
  `Deconstruct`-mode RCD can strip anything with that component (walls, windows, doors, cables, catwalks
  all have it upstream). To keep the roof hammer roof-only, added a marker component
  `RCDRoofOnlyComponent` (`Content.Shared/_Respiral/RCD/Components/`) + `RCDRoofOnlySystem`
  (`Content.Shared/_Respiral/RCD/`) that subscribes to `DoAfterAttemptEvent<RCDDoAfterEvent>` on that
  marker and cancels the operation (mode == Deconstruct only) unless the target has tag `MedievalRoof`
  (and always rejects tile deconstruction, `target == null`). This runs *after* the do-after has already
  started (no earlier hook exists in `RCDSystem.OnAfterInteract` for other systems to veto), so on an
  invalid target the effect briefly flashes before being cancelled on the next tick — cosmetic only.
- Charges: `LimitedCharges maxCharges: 20`, refilled by the existing `MedievalConstructionToolsAmmo`
  stack (`Entities/Objects/ProcessedMaterials/production.yml`) — it already carried `RCDAmmo` (30 charges
  flat, independent of stack count) and is consumed as a generic "tool ammo" material across dozens of
  *hand* construction graphs (roofs, furniture, doors...), but no entity had `RCDComponent` before this,
  so that component was previously dead code. Reusing it here ties that loose end together instead of
  inventing a second ammo item.
- Craftable via lathe: `MedievalRoofHammerRecipe` (`Recipe/recipe.yml`, needs
  `MedievalBasicBlacksmithing: Mastery`), in packs `MedievalAnvil` and `MedievalSmelter`
  (`Recipe/packs.yml`), next to `MedievalConstructionToolsRecipe`.
- Known gap: roofs have no `Fixtures`/collision (`MedievalBaseRoof` sets `Physics canCollide: false`), so
  the stock RCD "occupied tile" collision check can't block building two different roof materials on the
  same tile (only exact-duplicate-prototype stacking is blocked, by the stock RCD "identical entity" rule).
  Not fixed — out of scope, matches existing hand-construction behavior.

## Crafting items (objectType: Item) rather than structures

Items can also be crafted via Construction (not only structures/lathe):
- Recipes: `Construction/items.yml` (`objectType: Item`, `category: construction-category-tools`,
  no `placementMode`/`conditions`).
- Graphs: `Construction/Graph/Items/*.yml` (`torch.yml`, `weapons.yml`).
- The result entity (`node.entity`) **must** have `- type: Construction graph: <graph> node: <node>`.
- The `material:` step references a **stack prototype** (id from `Entities/Objects/**/stack.yml`), e.g.
  `MedievalRawStone`, `MedievalRodWood`, `MedievalRawCoal`, `MedievalBoard`.

Examples: torch (`MedievalSimpleLightTorch`), primitive weapons
(`MedievalCraftStoneAxe`/`MedievalCraftHandmadeSpear` → `MedievalStoneAxe`/`MedievalHandmadeSpear`,
raw material `MedievalRawStone` + `MedievalRodWood`; see combat.md).

## Railings — wood and stone from one grayscale RSI (2026-06)

Sprite `_Respiral/Structures/railing.rsi` (ledniky, CC-BY-SA-3.0) — **grayscale**, size 32×48,
states `side`/`corner`/`corner_small`/`round` (4 directions each; PNG 64×96 = 2×2 frame grid).

- **Wood and stone = the same RSI + `Sprite.color`** (like ladders/`stairs.yml`):
  wood `#8B4513` (= `MedievalProcessedWood.color`), stone `#808080` (= `MedievalProcessedStone.color`).
- Entities: `Entities/Structures/Walls/railing.yml`. Bases `MedievalBaseRailing` →
  `MedievalBaseRailingWood`/`MedievalBaseRailingStone` (set color, `damageModifierSet`,
  `Destructible`: wood 300/`WoodDestroy`, stone 500/`WoodDestroyHeavy`), 4 shapes per material
  (`MedievalRailing{Wood,Stone}{Side,Corner,CornerSmall,Round}`).
- Sprite height 48px → `Sprite.offset: 0,0.25` so the railing stands on the tile (can be tweaked).
- Climbable (`Climbable`), doesn't block sight/bullets (collision only `MidImpassable`+
  `LowImpassable`). Each shape carries `- type: Construction graph/node` (mandatory).
- Crafting: graphs `Construction/Graph/Structure/railing.yml` (`MedievalRailingWood` —
  `MedievalBoard`×1; `MedievalRailingStone` — `MedievalProcessedStone`×1, each node
  deconstructs via `Prying` returning the material), recipes `Construction/railing.yml`
  (`category: construction-category-structures`, `objectType: Structure`, no `name` →
  name taken from the entity). Recipe id = entity id (valid: different prototype types).

## Decorations without crafting (destructible)

`MedievalHangingMouse` (`Entities/Structures/Decoration/hanging_mouse.yml`, sprite
`_Respiral/Objects/Misc/hanging_mouse.rsi`) — just a destructible decoration, **without `Construction`**
(placed on the map, not crafted). Pattern: `parent: BaseStructure` + overriding
`Fixtures: {}` (remove the base's blocking collision) + `Damageable`/`Injurable: MedievalInorganic` +
`Destructible` (40 HP → `Destruction` + sound). Placed only on maps.
