# Round Goal (medieval station goal)

Medieval analogue of SS14's round-start "station goal paper on the captain's fax".
Upstream SS14 in this repo has **no** station-goal system — this is built from scratch.

## Concept

At round start a sealed scroll with the settlement's task for the cycle "teleports" onto
a **magic teleportation table** (`MedievalTeleportTable`) — a reskinned fax machine. The
sender in lore is **the Viscount** (Виконт).

## Pieces

| Piece | Path |
|---|---|
| `MedievalRoundGoalPrototype` (`type: medievalRoundGoal`) | `Content.Shared/_Respiral/RoundGoal/MedievalRoundGoalPrototype.cs` — just `id` + `text: LocId` |
| `MedievalRoundGoalReceiverComponent` (server marker) | `Content.Server/_Respiral/RoundGoal/MedievalRoundGoalReceiverComponent.cs` |
| `MedievalRoundGoalSystem` | `Content.Server/_Respiral/RoundGoal/MedievalRoundGoalSystem.cs` |
| Goal pool (8 goals) | `Resources/Prototypes/_Respiral/round_goals.yml` |
| Goal texts + paper name + stamp name | `Resources/Locale/{en-US,ru-RU}/_Respiral/round_goal/round_goal.ftl` |
| Teleport table entity | `Resources/Prototypes/_Respiral/Entities/Structures/Machine/teleport_table.yml` |
| Placeholder sprite | `Resources/Textures/_Respiral/Structures/Machines/teleport_table.rsi` (copied from `paperweight.rsi` — needs real art) |

## How delivery works

`MedievalRoundGoalSystem` subscribes to `RulePlayerJobsAssignedEvent` (same hook upstream
`StationGoalPaperSystem` uses — maps loaded, jobs assigned). Picks a random
`MedievalRoundGoalPrototype`, builds a `FaxPrintout` (prototype `MedievalPaper`, stamp state
`paper_stamp-generic`, `StampDisplayInfo` = "seal of the Viscount"), and calls
`_fax.Receive(uid, printout, ...)` on every entity with
`MedievalRoundGoalReceiverComponent` + `FaxMachineComponent`.

`FaxSystem.Receive` enqueues the printout; the fax `Update` loop plays the print sound,
runs the animation, then `SpawnPaperFromQueue` spawns the paper on the table's tile with the
content + stamp applied.

## Teleport table = reskinned fax

`MedievalTeleportTable` (parent `MedievalBaseMachine`) carries the upstream `FaxMachine` +
`FaxUiKey`/`FaxBoundUi` UI + `ItemSlots`/`Paper` `ContainerSlot` + `DeviceNetwork` on the
`Fax` frequency. So tables can send scrolls to each other exactly like faxes.

Differences from `FaxMachineBase`:
- **No power** — no `ApcPowerReceiver`/`BaseMachinePowered`/`ActivatableUIRequiresPower`/
  `DeviceNetworkRequiresPower`. Required a 1-line upstream edit (see below).
- Sounds: `/Audio/Effects/teleport_{arrival,departure}.ogg`.
- `printPaperId: MedievalPaper`.

### Fax animation states (how the fax drives the sprite)

The fax has 3 `FaxMachineVisualState`s, set by `FaxSystem.UpdateAppearance`:
- `Normal` — idle.
- `Inserting` — plays while a sheet is drawn into the paper slot
  (`OnItemSlotChanged` → `InsertingTimeRemaining = InsertionTime` 1.88s, slot locked). The
  client `FaxVisualsSystem` runs a `SpriteFlick` on the layer keyed
  `enum.FaxMachineVisuals.VisualState`: flick to `FaxMachineComponent.InsertingState`
  (default `"inserting"`, or `FaxableObjectComponent.InsertingState` of the inserted item —
  e.g. `inserting_mouse`) for 2.4s, then back to `"icon"`.
- `Printing` — plays while an incoming message prints
  (`PrintingTimeRemaining = PrintingTime` 2.3s, `PrintSound`), then `SpawnPaperFromQueue`.
  Mapped in YAML by `GenericVisualizer`.

So the single sprite layer must be **mapped `map: [ "enum.FaxMachineVisuals.VisualState" ]`**.
`GenericVisualizer` maps `Printing → {state: teleporting}`, `Normal → {state: icon}`; the
`inserting`/`icon` states are reached by the client flick, not the visualizer.

`teleport_table.rsi` (32×32, real art): `icon` = idle table; `teleporting` = 12-frame
teleport animation (delays 0.18 → 2.16 s total); `inserting` = same 12 frames (reused so
the insert flick has a valid state for any faxable item, incl. `inserting_mouse` fallback
misses gracefully). Two visual concepts: idle vs. teleport swirl.

### Sound/animation sync

`FaxMachineComponent.PrintingTime` (how long `Printing` shows before the paper spawns) was
`[ViewVariables]` only — patched to `[DataField]` upstream. The table sets
`printingTime: 2.16` = length of `/Audio/Effects/teleport_arrival.ogg`, and the RSI
`teleporting` delays sum to 2.16, so sound + one animation loop finish together
(both start in the same frame in `ProcessPrintingAnimation`). If the sound file is swapped,
re-measure (`ffprobe`) and update `printingTime` + the delays together.

`MedievalTeleportTableViscount` — child with `MedievalRoundGoalReceiver`; this is the one
mappers place in the Viscount's hall. Base `MedievalTeleportTable` is a plain
send/receive table.

## Upstream edit

`Content.Server/Fax/FaxSystem.cs` `Update()` — the query was
`EntityQueryEnumerator<FaxMachineComponent, ApcPowerReceiverComponent>` with a hard
`!receiver.Powered` continue. Changed to query `FaxMachineComponent` alone and make the
power check conditional (`TryComp<ApcPowerReceiverComponent>` — if present, still require
power). Marked `// Respiral - ...`. Without this a powerless fax never animates/prints.
See [[upstream_patches]].

## Also touched

- `MedievalPaper` (`bureaucracy` / paper.yml) gained `- type: FaxableObject` so players can
  insert/send medieval paper through a table. See [[bureaucracy]].

## Gaps / TODO

- **No real sprite** for the teleport table (placeholder = paperweight).
- **Not placed on any map** — needs `MedievalTeleportTableViscount` in the Viscount's hall
  and plain `MedievalTeleportTable`s elsewhere for the feature to function in a round.
- **Not craftable** — map/admin spawn only (like the CentComm fax). Add a `MedievalMachine`
  graph board + lathe recipe if player construction is wanted.
- **No announcement** — only the scroll appears. A chat/magic-ball announcement from the
  Viscount would be a cheap add (see [[communications]]).
- Unweighted random pick; no per-goal weight or round-type gating.
