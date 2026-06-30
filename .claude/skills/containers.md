# Containers & Barrels

## EntityStorage (Crates, Barrels, Lockers)

`EntityStorageComponent` — stores entities inside (not a UI inventory, but physical disappearance inside).

### Key fields
| Field | Default | Description |
|---|---|---|
| `airtight` | `true` | Airtightness: if `true` — traps the air, a person suffocates |
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
