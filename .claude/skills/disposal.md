# Disposal — a power-free medieval variant

## How disposal pipes work with tiles (important)

Disposal pipes are **separate anchored entities**, NOT part of the
tile (unlike tile-defs). They are placed by `SnapgridCenter` — one per tile.

- **The network is built from adjacency and directions.** Each pipe segment
  (`DisposalTubeComponent`) has a set of connectable directions (taken from
  `connectableDirections`/the segment shape, rotated with the entity). A connection
  forms when an **adjacent tile** has a pipe whose direction points back.
  No electricity needed — the system is pneumatic/passive.
- **Network entry = trunk.** For a unit to dump its contents into the pipes, the **same tile**
  under the unit must have a pipe with `DisposalEntryComponent` — that's a `DisposalTrunk`
  (the `trunk` node of the `DisposalPipe` graph). `SharedDisposalUnitSystem.TryFlush` iterates
  entities at the unit's coordinates (`_map.GetLocal`) looking for `DisposalEntryComponent`.
  No trunk under the unit → no flush (the garbage stays inside).
- **Moving the contents:** when flushing, a `DisposalHolder` is created that
  "rides" pipe segments from neighbor to neighbor until an exit (`DisposalOutlet`/`DisposalRouter`
  by tags). Also power-free.
- Upstream pipes (`DisposalPipe`, `DisposalTrunk`, `DisposalBend`, `DisposalRouter`,
  `DisposalOutlet`, `DisposalInlet`) **are unpowered by themselves** — they can be
  used with the medieval unit as-is.

## Power: why the medieval unit works without electricity

`SharedDisposalUnitSystem.CanFlush` requires `_power.IsPowered(owner)`.
`SharedPowerReceiverSystem.IsPowered` returns **true** if the entity has **no**
`ApcPowerReceiver` (`ResolveApc` == false). So it's enough NOT to inherit
`BaseMachinePowered` (which adds `ApcPowerReceiver`+`ExtensionCableReceiver`) and not to
attach `PowerSwitch` — the flush is immediately "powered".

Pressure (`NextPressurized`/`PressurePerSecond`) recovers **on a timer** in
`Update`, independent of power. `UpdateVisualState` also draws charging/full only
when `IsPowered` — which for a power-free unit is always true, so the overlays work fine.
The `enum.PowerDeviceVisuals.Powered` blocks in `GenericVisualizer` were removed (no power).

## Tiles: visibility (subfloor) and buildability

### Why MedievalLattice = isSpace
`isSpace` (YAML) → property `ContentTileDefinition.MapAtmosphere` (`ContentTileDefinition.cs:106`).
This is an **atmospheric** flag: the tile is open to the map atmosphere (vacuum/space), i.e. it does NOT hold
air. A bare lattice is a structural floor you walk on, but it doesn't seal a room;
a real floor on top seals it (`MedievalFloorStone`). It has nothing to do with
graphics. Side effect: the `TileNotBlocked` build condition (default
`failIfSpace: true`) refuses to place heavy anchoring structures on a bare lattice.

### Visibility of subfloor entities (pipes/cables)
`DisposalPipeBase` (upstream) has `SubFloorHide`. The rule in
`SharedSubFloorHideSystem.HasFloorCover` (`SharedSubFloorHideSystem.cs:167`):
the entity is HIDDEN ⇔ the tile has `IsSubFloor == false` (i.e. a real floor on top).

| Tile | `isSubfloor` | pipe visible? | can build pipe? | can build unit (tank)? |
|---|---|---|---|---|
| `MedievalLattice` | true (`isSpace:true`) | ✅ visible | ✅ yes (pipes have no `TileNotBlocked`) | ❌ no (unit has `TileNotBlocked`+`failIfSpace`) |
| `MedievalDirt` | true (`sturdy:false`) | ✅ visible | ✅ yes | ❌ no (`failIfNotSturdy`) |
| `MedievalFloorStone` | false | ⛔ hidden under floor | ✅ yes | ✅ yes |

Result: disposal pipes are automatically visible on a bare lattice/dirt and hidden under a stone
floor laid over the lattice — this is standard subfloor mechanics, nothing to change.

### Building/anchoring on space tiles
- The `DisposalPipe` recipe (upstream) does NOT contain `TileNotBlocked` — only
  `canBuildInImpassable: false`. `AnchorableSystem.TileFree` (`AnchorableSystem.cs:344`)
  only checks collisions with other anchored entities, NOT `IsSpace`. → pipes build on
  `MedievalLattice` without issue.
- The unit tank (`MedievalDisposalUnit`) is a surface structure (no `SubFloorHide`),
  placed on a normal floor. `TileNotBlocked` on it is correct; it shouldn't be placeable on
  a bare lattice anyway. To allow it — `failIfSpace:false`/`failIfNotSturdy:false`.
- Do NOT remove `isSpace` from `MedievalLattice` — otherwise the bare lattice would start holding atmosphere.

## Respiral prototypes

| ID | File | Notes |
|---|---|---|
| `MedievalDisposalMachineFrame` | `Entities/Structures/Piping/disposal.yml` | frame; visualizer without cable/electronics |
| `MedievalDisposalUnitBase` (abstract) | same | `parent: BaseMachine` (no power) |
| `MedievalDisposalUnit` | same | crafting target entity |
| `MedievalDisposalPipeBase` (abstract) | `Entities/Structures/Piping/disposal_pipes.yml` | `SubFloorHide`+`DisposalTube`, sprite `_Respiral/...disposal.rsi` |
| `MedievalDisposalPipe` / `…Bend` / `…Junction`(+`Flipped`) / `…YJunction` / `…XJunction` / `…Trunk` | same | network geometry; trunk = entry (`DisposalEntry`) |
| `MedievalDisposalPipeBroken` | same | broken state (node `broken`) |

- Unit graph: `MedievalDisposalMachine` (`Construction/Graph/Structure/disposal.yml`):
  `start → frame → frame_unit → disposal_unit`. The cables/electronics steps from upstream
  removed. Material — `MedievalBoard` (processed wood). Deconstruction via `Prying`.
- Pipe graph: `MedievalDisposalPipe` (`Construction/Graph/Structure/disposal_pipes.yml`):
  `start → {pipe,trunk,bend,junction,junctionflipped,yJunction,xJunction}` + a cycle
  through the T-junctions via `Screwing`. Material `MedievalBoard` ×2, deconstruct `Prying`.
- Material = processed wood: graphs/drops use the **stack `MedievalBoard`**
  (the material prototype is `MedievalProcessedWood`, its `stackEntity: MedievalBoard1`).
- Recipes: `Construction/piping.yml`. The unit — with `TileNotBlocked`; **pipes — without it**
  (subfloor segments build on bare lattice/dirt).
- Electronics not ported: tagger/signaller/router/signal-router (need
  `DeviceNetwork`/`DeviceLink`); the mailing unit (needs a wired network).
- ⚠️ The ID `MedievalDisposalPipe` deliberately coincides for entity/constructionGraph/construction
  — these are DIFFERENT prototype types, no conflict (like the railings).
- Damage: `MedievalWood` / `MedievalInorganic`, sound `WoodDestroy`. Unit/frame Destruct
  200; pipes — like upstream (Breakage@50 → node `broken`, Destruction@100).
- Pricing: NOT `StaticPrice`. Crafted items use `MedievalPhysicalPriced` +
  `PhysicalComposition` (`materialComposition` in units of the **material** `MedievalProcessedWood`,
  1 board = 100). Unit base = 300 (3 boards), frame = 200, pipe base = 200 (2 boards).
  Sell price = material_value × 1.2 (see `trade_console.md`).

## Sprites (duplicated into the _Respiral namespace)

- `_Respiral/Structures/Piping/disposal.rsi` — a full copy of upstream (the user will replace it later).
- `_Respiral/Structures/Piping/high_pressure_machine_frame.rsi` — a copy WITHOUT electronic
  layers: `frame_cables.png` and `frame_electronics.png` removed (and their `meta.json` entries).
  Remaining: `frame`, `frame_unit`, `frame_inlet`, `frame_outlet`.

## Not done / TODO

- The mailing unit is NOT ported: requires `DeviceNetwork`/`WiredNetworkConnection`
  (wired network) — incompatible with the power-free concept.
- Sorting not ported: tagger / router(+flipped) / signaller / signal-router.
- No medieval `DisposalOutlet`/`Inlet` (upstream's are map-only anyway,
  not buildable). Without an exit, items are dumped at a dead-end pipe segment.
