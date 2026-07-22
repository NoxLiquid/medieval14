# Fluids — liquids, puddles, evaporation, absorption

## Evaporation (upstream)

- `EvaporationComponent` (`Content.Shared/Fluids/Components/`) is added to puddles that contain evaporating reagents.
- Logic in `SharedPuddleSystem.Evaporation.cs`: ticks once per second, drains reagents by their average `EvaporationSpeed`.
- `ReagentPrototype.EvaporationSpeed` (`> 0`) marks a reagent as evaporating.
- Useful public `SharedPuddleSystem` helpers: `GetEvaporatingReagents`, `GetEvaporationSpeeds`, `GetAbsorbentReagents`, `CanFullyEvaporate`.
- Empty puddles are deleted automatically: `UpdateChemicals` → `SolutionChangedEvent` → `SharedPuddleSystem.OnSolutionUpdate` (queues deletion when `Volume <= 0`).

## Tile absorption (Respiral)

- **Tile flag**: `ContentTileDefinition.Absorbent` (bool, DataField) — whether the tile can soak up liquids. Lives in the `// Respiral-start/-end` section of `Content.Shared/Maps/ContentTileDefinition.cs`.
- **System**: `Content.Server/_Respiral/Fluids/PuddleAbsorptionSystem.cs` (server-only, GPL).
- **Marker**: `Content.Server/_Respiral/Fluids/Components/TileAbsorbingComponent.cs` — marks a puddle sitting on an absorbent tile.

### Behavior
- **ALL reagents are absorbed**, not only evaporating ones. Removal via `solution.SplitSolution(amount)` (drains volume from every reagent proportionally to its share).
- Rate: `AbsorptionRate` units per tick, `AbsorptionCooldown` interval (tunable constants in the system).
- The puddle solution is fetched via `SharedSolutionContainerSystem.TryGetSolution(uid, "puddle", …)` (name = `PuddleComponent.SolutionName` default; the component is `Access`-restricted to `SharedPuddleSystem`, so the name is hardcoded as the `PuddleSolutionName` constant).
- Empty puddles are removed by upstream: `UpdateChemicals` → `SolutionChangedEvent` → `SharedPuddleSystem.OnSolutionUpdate`.

### Optimization (important)
- The tile under a puddle is checked **once** on `PuddleComponent` + `MapInitEvent` (`OnPuddleMapInit`): `TurfSystem.GetTileRef` → `GetContentTileDefinition(tileRef).Absorbent`. If absorbent, `TileAbsorbingComponent` is added.
- `Update` iterates only `TileAbsorbingComponent + PuddleComponent`, with no per-tick grid lookup. The marker is auto-cleaned when the puddle is deleted (ECS), so no manual collection is needed.
- Marker over a raw `HashSet`: auto-cleanup + queryable, same pattern as upstream `EvaporationComponent`.

### Limitations / notes
- The tile check is one-time (at puddle creation). If the tile beneath an existing puddle is changed (floor re-tiled), the absorbing status goes stale until the puddle is recreated. Puddles are short-lived, so this is considered acceptable. Subscribe to `TileChangedEvent` if strictness is needed.
- Absorption is independent of evaporation: on an absorbent tile, an evaporating reagent dries out from both mechanisms at once.
