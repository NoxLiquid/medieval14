# Containers & Barrels

## EntityStorage (Crates, Barrels, Lockers)

`EntityStorageComponent` — stores entities inside (not a UI inventory, but physical disappearance inside).

### Key fields
| Field | Default | Description |
|---|---|---|
| `airtight` | `true` | Airtightness: if `true` — traps the air, a person suffocates. Wardrobes/closets a player climbs into (e.g. `MedievalWardrobeCarved`, `MedievalWardrobeIron`) MUST set `airtight: false`, otherwise the sealed occupant takes pressure/suffocation damage |
| `open` | `false` | Open/closed state |
| `capacity` | 30 | Max number of items |

### Open/close visuals (EntityStorageVisuals)

**Critically important:** `EntityStorageVisualizerSystem` checks for the presence of a `StorageVisualLayers.Door` layer in the sprite. If the layer is absent — the entire visualization block is skipped, the sprite doesn't change.

Two approaches:
1. **Base + Door** (like MedievalBaseCrate): Base — body, Door — lid/door
2. **Base swap** (like the barrel): Door is present formally (hidden), Base is swapped via `stateBaseClosed`/`stateBaseOpen`

```yaml
# Crate pattern (MedievalBaseCrate):
layers:
- state: base
  map: ["enum.StorageVisualLayers.Base"]
- state: closed
  map: ["enum.StorageVisualLayers.Door"]
# EntityStorageVisuals:
stateDoorOpen: open
stateDoorClosed: closed

# Barrel pattern (MedievalBarrel):
layers:
- state: icon
  map: ["enum.StorageVisualLayers.Base"]
- state: icon
  map: ["enum.StorageVisualLayers.Door"]
  visible: false
# EntityStorageVisuals:
stateBaseClosed: icon
stateBaseOpen: barrel_open
```

### Collision
Crates use `CrateMask`, not `MachineMask`.

### Using EntityStorage (capacity:1 + whitelist) as a single-item "slot" — gotcha

`StorageVisuals.HasContents` (bool) is set automatically with zero custom code
(`SharedEntityStorageSystem.ModifyComponents`, `Content.Shared/Storage/EntitySystems/SharedEntityStorageSystem.cs:497`),
but it is ONLY refreshed from `OpenStorage()`/`CloseStorage()` — not on every container mutation.

**Critical gotcha — leaving `open: true` does NOT give free-form insertion.**
`Insert()` (line 301-321) branches on `component.Open`:
- if `Open == true` → `TransformSystem.DropNextTo(toInsert, container)` — item is just dropped on the
  floor next to the entity, it is **not** added to `Contents`, `HasContents` stays false, `MapInit`/whitelist
  are never consulted for it.
- if `Open == false` → actually inserts into the `Contents` container.

The only code path that populates `Contents` is `CloseStorage()` (`SharedEntityStorageSystem.cs:249-299`):
on close, it does an `EntityLookupSystem` scan within `EnteringRange` of `EnteringOffset` around the
storage, and for each nearby entity passing `CanInsert` (whitelist/blacklist/size/capacity) calls
`AddToContents` → `Insert`. So the real UX is: open the "slot", place the item within ~`EnteringRange`
(default 0.18) of the entity, then close it (click again / verb) — items don't teleport-insert on drop or
drag-and-drop directly onto a closed storage; there is no `InteractUsing`/drag-drop handler wired up in
`SharedEntityStorageSystem`/`Content.Server/Storage/EntitySystems/EntityStorageSystem.cs`.

**`capacity: 1` blocks a second item cleanly.** `CanInsert` (line 353) checks
`Contents.ContainedEntities.Count >= Capacity` and the `CloseStorage` scan loop also `break`s once
capacity is reached (line 287) — so at most one item is ever swept in per close, and any further
`Insert`/`AddToContents` call from other code returns false while full.

**Whitelist by tag:** `EntityStorageComponent.Whitelist` is a standard `EntityWhitelist`
(`Content.Shared/Whitelist/EntityWhitelist.cs`) — supports `tags: [ ... ]` (`List<ProtoId<TagPrototype>>`)
or `components: [ ... ]`, fully YAML-driven, no code needed. Default whitelist (if unset) is
`components: [MobState, Item]` — override it explicitly if you want a pure-tag slot.

**Bottom line:** a single-item "accessory slot" built purely from `EntityStorageComponent` works with
zero C#, but it inherits the crate/locker open-place-close interaction model — it is not a instant
"drop item here" slot unless you also write a custom insert path (e.g. subscribe `InteractUsingEvent`
and call `EntityStorageSystem.Insert`/`AddToContents` directly), which WOULD require custom C#.

## Cart (MedievalCart) — EntityStorage HasContents used for real

`Resources/Prototypes/_Respiral/Entities/Structures/cart.yml`. Sprite: `_Respiral/Structures/cart.rsi`
(`cart`/`cart_empty`, 4 directions each). Parent `BaseStructureDynamic` (dynamic body, `Pullable`,
`Anchorable` — the standard "heavy movable prop" base, also used by crates). No `EntityStorageVisuals`
(no `Door` layer at all — open/closed has no dedicated art here, only contents-vs-empty does):
```yaml
- type: EntityStorage
  capacity: 60
- type: GenericVisualizer
  visuals:
    enum.StorageVisuals.HasContents:
      base:
        True: { state: cart }
        False: { state: cart_empty }
```
Confirms the "EntityStorage as single-item slot" gotcha above in reverse: for a genuine multi-item
container (not a 1-slot equipment stand) the open→place-nearby→close interaction model IS the desired
crate-like UX, so `HasContents` needs zero code here — it's the correct tool, unlike for the horse's
saddle (single accessory slot, wanted instant equip → used Inventory/Clothing instead, see [[inventory]]
and [[mobs]]).

`density: 900` on the `Fixtures` (bounds sized for the 64×64/2-tile sprite, `-0.9,-0.9,0.9,0.9`) is the
"hard for one human to drag" lever — this repo has no explicit strength/mass gate on `Pullable`, so a
heavy `Dynamic` body just resists acceleration harder under Robust's normal physics (higher mass, same
pull force ⇒ slower actual movement). For reference, other _Respiral dynamic (non-static) fixtures range
~50-250 density; 900 is deliberately an outlier to make it feel like the heaviest common pullable prop.
`WoodDestroyHeavy` sound collection (`Resources/Prototypes/SoundCollections/destruction.yml`) matches
other big wooden structures (`barricades.yml`, `girder.yml`, `door.yml`) on `Destructible`.

## Barrel — hybrid EntityStorage + SolutionContainer

`BarrelComponent` (marker) — `Content.Shared/_Respiral/Barrel/`.

### Requirements
1. Stores items (EntityStorage)
2. Stores liquid (SolutionContainer: `drink`, 500u)
3. Closed barrel: can't pour in/out, fill layer hidden
4. Open barrel: can pour in/out, layer visible
5. `airtight: false` — a person inside doesn't suffocate

### Implementing the transfer block

`BarrelSystem` (Shared) subscribes to `SolutionTransferAttemptEvent` — cancels the transfer if `EntityStorageComponent.Open == false`.

### Implementing the Fill-layer hide

**Visualizer ordering problem:**
- `SolutionContainerVisualsSystem` unconditionally sets `Fill` → `visible: true` when liquid is present
- If our visualizer runs first — `SolutionContainerVisualsSystem` overrides the hide

**Solution:** `BarrelVisualizerSystem` (Client, an `EntitySystem`, NOT `VisualizerSystem<T>`) subscribes to `AppearanceChangeEvent` with `after: [typeof(SolutionContainerVisualsSystem)]`.

**Why not `VisualizerSystem<T>`:** the base `Initialize()` subscribes the handler without an `after:` parameter. Skipping `base.Initialize()` violates `RA0028`. The only correct option is an `EntitySystem` with manual `[Dependency]` and explicit ordering.

### SolutionContainer-on-structures pattern

```yaml
- type: SolutionContainerManager
  solutions:
    drink:
      maxVol: 500
- type: SolutionContainerVisuals
  maxFillLevels: 5
  fillBaseName: liq       # sprites: liq1, liq2, liq3, liq4, liq5
  changeColor: true
- type: RefillableSolution
  solution: drink
- type: DrainableSolution
  solution: drink
- type: ExaminableSolution
  solution: drink
- type: SolutionTransfer
  canChangeTransferAmount: true
  maxTransferAmount: 50
```

To scoop from the barrel: the item in hand must have `SolutionTransferComponent` + `RefillableSolutionComponent` (cups and mugs have them via `DrinkBaseCup`).

### Solutions Refactor (upstream #43412) — new solution model

Upstream moved from `SolutionContainerManager.solutions.<id>` to a per-solution component
`Solution`. Food/drink bases now provide the needed solution id themselves:
- `SolutionFood` → `- type: Solution` with `id: food`; part of `FoodBase`/`FoodProduceBase`.
- `SolutionDrink` → `- type: Solution` with `id: drink`; part of `DrinkBase`.

**Conflict symptom:** if an entity inherits such a base AND also sets the old
`SolutionContainerManager.solutions.food/drink`, the engine logs during porting
`[WARN] system.solution_container: Attempted to port a solution id: food ... but the
entity already had a solution with that id`. In the integration test pool a WARN = failure.

**Fix — replace the old block with the new `Solution` component (one level less nesting):**

```yaml
# was (legacy):
- type: SolutionContainerManager
  solutions:
    food:
      maxVol: 10
      reagents:
      - ReagentId: JuiceCarrot
        Quantity: 4
# now (upstream pattern, cf. FoodCarrot):
- type: Solution
  solution:
    maxVol: 10
    reagents:
    - ReagentId: JuiceCarrot
      Quantity: 4
```

`id` (`food`/`drink`) is pulled from the base; `maxVol`/`reagents` are merged into it.
For multiple solutions on one entity — multiple `Solution` components with different `id`s.

**When NOT to touch:** `SolutionContainerManager` stays correct if the entity
defines its own solution AND does NOT inherit a base with the same `Solution.id`
(barrels/structures in `barrel.yml`, `alchem_table.yml`, `soil.yml`, `production.yml`,
`cow.yml`, `goat.yml` — no conflict, they weren't converted).

Converted (mentioned in middle-food/drink warnings): `herb.yml`, `drinks.yml`
(2 bases — `MedievalDrinkBaseCup`, `MedievalDrinkBaseBottle` — fix all their child cups/bottles),
`Consumable/Food/produce.yml`, `RawMaterials/Seeds/produce.yml`.

### Files
| Path | Contents |
|---|---|
| `Content.Shared/_Respiral/Barrel/BarrelComponent.cs` | Marker component `[NetworkedComponent]` |
| `Content.Shared/_Respiral/Barrel/BarrelSystem.cs` | Block `SolutionTransferAttemptEvent` when storage closed |
| `Content.Client/_Respiral/Barrel/BarrelVisualizerSystem.cs` | Hide Fill layer when storage closed |
| `Resources/Prototypes/_Respiral/Entities/Structures/Furniture/barrel.yml` | `MedievalBarrel` prototype |
| `Resources/Locale/ru-RU/_Respiral/barrel.ftl` | Popup localization |
