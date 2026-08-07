# ZLevels (Z-levels)

A system of vertical levels, ported from CrystallEdge (MIT).

## Architecture

- Code: `Content.Server/_Respiral/ZLevels/Core/CEZLevelsSystem.cs`, components prefixed `CE`
- On `StationPostInitEvent` the system loads maps from `CEStationZLevelsComponent.MapsBelow` / `MapsAbove`
- Each map is added as a separate grid to the station via `StationSystem.AddGridToStation()`
- All Z-levels are combined into a `ZNetwork` with the corresponding depth levels

## Prototypes

- Defined in the map's YAML file (e.g. `verdant_vale.yml`)
- The `CEStationZLevels` component sets the lists of maps above/below the main one
- A `zMap` prototype is also declared with all the maps for Z-navigation

## Shared components across the whole z-network

Instead of putting a map-scoped component (e.g. `LightCycle`, day/night) on every level by hand, declare it once and let the network fan it out to every level map:

- **Round start (real game):** `CEStationZLevels.zLevelsComponentOverrides` (a `ComponentRegistry`) on the `gameMap` station. `CEZLevelsSystem.OnStationPostInit` → `CreateMapNetwork(ZLevelsComponentOverrides)` stores it on `CEZMapNetworkComponent.Components`.
- **Mapping command:** `zMap` prototype's `components:` registry (used by `CEMappingZNetworkCommand` / `CEGameMapMappingZNetworkCommand` → `CreateMapNetwork(indexedZMap.Components)`). Keep it in sync with the station overrides if you rely on the mapping path.
- **Application:** `CEZLevelMappingSystem` calls `EntityManager.AddComponents(mapUid, network.Comp.Components)` on each map's `MapInitEvent` / when a map is added to the network — so every level (above/core/below) gets the component.

`AddComponents` uses `removeExisting: true` (default), so a registry component **overwrites** any same-named component the level's map file already defines — the shared value always wins at round start.

Example — VerdantVale shares its whole surface environment via `verdant_vale.yml` → `CEStationZLevels.zLevelsComponentOverrides`: `LightCycle`, `MapLight`, `SunShadow`, `SunShadowCycle`, `Gravity`, `MapAtmosphere`. These were removed from the three map files (`medieval_verdant_vale{,_above,_below}.yml`) so there is a single source of truth.

## Map file layout

`Resources/Maps/_Respiral/` is split into one subfolder per map (e.g. `VerdantVale/`, `Empty/`), each holding its 3 z-level files (`<name>{,_above,_below}.yml`). `mapPath`/`mapsBelow`/`mapsAbove`/`zMap.maps` in the matching `Resources/Prototypes/_Respiral/Maps/<name>.yml` must reference the subfolder path. `RespiralMapTest` (`GameDataScrounger.FilesInDirectoryInVfs`) walks the directory recursively, so subfolders don't break map-loadable coverage.

## MedievalEmpty — blank 3-level template

`Resources/Prototypes/_Respiral/Maps/empty.yml` (`gameMap`/`zMap` id `MedievalEmpty`) is a blank mapping canvas mirroring VerdantVale's component wiring 1:1 (`CEStationZLevels` + identical `zLevelsComponentOverrides`, `StationNameSetup`, `StationJobs: MedievalPilligrim`, `Gravity`, `CEAutoGridGravity`) but with empty grids (`MapGrid.chunks: {}`, empty `tilemap`, no `Roof.data`, no built entities). Maps: `Resources/Maps/_Respiral/Empty/medieval_empty{,_above,_below}.yml`. Only the core map (`[1]`) carries `MapPrecipitation` and the 3 spawn points (`SpawnPointLatejoin`/`SpawnPointMedievalPilligrim`/`SpawnPointObserver`, all at the origin) — above/below have none, matching VerdantVale. `above` carries `Parallax: MedievalBlack` (per-map, never shared), `below` doesn't. Verified loadable via the YAMLLinter (`dotnet run --project Content.YAMLLinter -c Tools`).

What to share vs keep per-map:
- **Share** (identical/global across levels): `LightCycle`, `SunShadow`/`SunShadowCycle` (global sun), `Gravity`, `MapAtmosphere`. Env-look components like `MapLight` can be shared **only if all levels should look the same** — sharing forces the same ambient onto every level (e.g. an underground level goes bright). Here below is treated as an open/surface level, so `MapLight` is shared.
- **Never share** (per-map data): `Roof` (its `data:` is that level's own roof tilemap), `Parallax` (per-level sky), `MapGrid`/`Map`/`Transform` (per-entity), `MapPrecipitation` (single network-wide driver — put on one map only).

Caveats:
- `LightCycle` needs `MapLight` on the same map to have any effect (it drives `MapLightComponent.AmbientLightColor`); `OriginalColor` is captured per-map. Sharing `MapLight` too gives every level the same base tint + synchronized cycle.
- Sync: all levels compute from `RoundStartTime + Offset`; identical config ⇒ synchronized. `InitialOffset: true` re-randomizes per map ⇒ desync.
- Gameplay-critical shared comps (`Gravity`, `MapAtmosphere`) now live **only** in the override registry. A level map opened **standalone in the editor** (outside the z-network) will lack gravity/air until the network applies them. In production these maps always load via the station z-network, so it's fine; mirror them into the `zMap` `components:` if you edit via the mapping command.

## Known issues and solutions

### `Required properties not provided for` in Serilog logs
**Cause:** `StationNameSetup.mapNameTemplate` contains placeholders `{0}`, `{1}` without setting a `nameGenerator`. The station name stays unformatted. When `StationSystem.AddGridToStation` logs (line 350), Serilog interprets `{0}` and `{1}` as named template parameters.
**Solution:** Either remove the placeholders from `mapNameTemplate`, or add a `nameGenerator` to the prototype.
