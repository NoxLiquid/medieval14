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

`Resources/Prototypes/_Respiral/Maps/empty.yml` (`gameMap`/`zMap` id `MedievalEmpty`) is a blank mapping canvas mirroring VerdantVale's component wiring 1:1 (`CEStationZLevels` + identical `zLevelsComponentOverrides`, `StationNameSetup`, `StationJobs: MedievalRefugee`, `Gravity`, `CEAutoGridGravity`) but with empty grids (`MapGrid.chunks: {}`, empty `tilemap`, no `Roof.data`, no built entities). Maps: `Resources/Maps/_Respiral/Empty/medieval_empty{,_above,_below}.yml`. Only the core map (`[1]`) carries `MapPrecipitation` and the 3 spawn points (`SpawnPointLatejoin`/`SpawnPointMedievalRefugee`/`SpawnPointObserver`, all at the origin) — above/below have none, matching VerdantVale. `above` carries `Parallax: MedievalBlack` (per-map, never shared), `below` doesn't. Verified loadable via the YAMLLinter (`dotnet run --project Content.YAMLLinter -c Tools`).

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

### "File not found" for a map that visibly exists (2026-08)
**Cause:** a new station map was created by copy-pasting `empty.yml`/`medieval_empty*.yml` as a template (e.g. `Edgehaven`) and only the `zMap` block's paths + ids were renamed — the `gameMap.mapPath`/`mapsBelow`/`mapsAbove`/`stations:` key/`StationNameSetup.mapNameTemplate` and the map files' own `BecomesStation.id`/`MetaData.name` were left as stale `Empty`/`/Maps/_Respiral/Empty/...` copies. The YAMLLinter catches the path mismatch (`File not found. (/Maps/_Respiral/Empty/medieval_edgehaven.yml)`); a mismatched `BecomesStation.id` vs. the `stations:` key would NOT be caught by the linter (both are valid strings) — check it manually.
**Solution:** when cloning `empty.yml` for a new map, grep the new prototype + its 3 map files for the old map's name/`Empty` and rename every occurrence, not just the `zMap` block.

### Z-level travel disables a vanilla `RelativeRotation` reset — bites mounted riding (2026-08)
**Where:** `Content.Shared/_CE/ZLevels/Core/EntitySystems/CESharedZLevelsSystem.Movement.cs`, block
`// Respiral-ZLeveling-Edit-start/end`. Upstream `SharedMoverController.Input.cs` (`OnInputParentChange`)
normally resets `InputMoverComponent.RelativeRotation`/`TargetRelativeRotation` to `Angle.Zero` whenever
`oldMapId != mapId` (i.e. on any cross-map teleport) — that reset is commented out here for Z-level
travel, so `RelativeRotation` instead falls through to the grid-changed cardinal-snap logic
(`SharedMoverController.Input.cs:211-228`), which can legitimately end up nonzero depending on the
old/new grid's relative world rotation.

Normally this is invisible (a player just corrects their own facing without noticing). It became a real
bug for [[mobs]]'s horse mount: once a player becomes a movement-relay *source* (`SetRelay`, used for
riding), `HandleMobMovement`'s relay branch (`SharedMoverController.cs:132-166`) returns before ever
reaching `TryUpdateRelative` again — so `RelativeRotation` freezes at whatever it was the instant relaying
started, and that frozen (possibly nonzero, courtesy of the disabled Z-level reset above) value gets
copied onto the mount every frame, permanently skewing `wishDir`→world-space conversion. Symptom: "the
horse moves in a direction unrelated to my keys after mounting." Fixed **in the upstream file** (marked
`// Respiral-Start/End`, `SharedMoverController.cs:132-143`): the relay-source branch now also calls
`TryUpdateRelative` on itself every tick (same `LerpTarget` gating as the normal, non-relay path), so its
`RelativeRotation` stays live instead of freezing at ride-start. See [[mobs]] "Mounts & saddle slot" for
the full riding feature and the sibling misprediction/AI bugs found alongside this one.

## Migration: re-port ZLevels from CrystallEdge (checklist, started 2026-09-05)

**Why:** our `_CE/ZLevels` is a ~July-2026 snapshot; CrystallEdge (`/home/user/workspace/crystall-edge`)
is ~1.5 months ahead (last ZLevels work 2026-09-02: `Zlevels removal API #447`, `ZLevels navmap #444`,
`Break the ceiling #434`). Namespaces are identical (`Content.*._CE.ZLevels.*`); **our `_CE/ZLevels` has
zero Respiral markers** (verified) — nothing original to preserve inside it. Decision (user): wipe our
`_CE` ZLevels code and replace with CE's.

### Comparison snapshot (CE vs ours)
- Files: CE 86, ours 77. Of 74 common: **41 identical, 33 diverged** (CE ahead — e.g. `CEZLevelsSystem.Maps.cs` +152 CE lines, Roof +52, Movement +38, Flight +36, View ~+35, CCVars +18).
- **12 files missing from ours (new CE subsystems):** Audio (`CEZLevelAudioSystem` + CCVars: per-level attenuation, floor occlusion), NavMap (`CENavMapGeometry`, `CEZLevelsNavMapControl`), DayCycle (`CEZNetworkTimeSystem` + Add/SetTime cmds), Pulling across Z (`CEZLevelPullingSystem`+Transition), Tiles (`CEZLevelToolTileSystem`), Mapping cmds (Insert/RemoveMapIntoZNetwork).
- **Our 2 "unique" files are not keepers:** `CEPvsOverride*` = CE's own code we merely relocated (CE keeps it at `Content.Server/_CE/PVS/`, byte-identical bar namespace); `CEToggleZLevelLookUpAction` = our old form, CE superseded it with the standard `ToggleActionEvent` + `LookUp` toggle on `CEZLevelViewerComponent`. `CEZLevelThrowingSystem` is the only common file where we had +5 lines — review, likely take CE's.

### Code dependency surface (closed set)
CE's ZLevels code references only **one** non-ZLevels `_CE` namespace: `Content.Server._CE.ZCollapse`
(grid-stability / "break the ceiling", 12 files, no further `_CE` deps) + `Content.Server._CE.PVS`
(2 files). So the copy set is **ZLevels + PVS + ZCollapse** — does NOT drag in the wider CrystallEdge
fork (which has ~60 `_CE` subsystems + huge resources — do NOT bring those).

### Migration steps — progress (2026-09-05)
1. [x] Deleted our `_CE/ZLevels/` (all 3 projects).
2. [x] Copied CE `_CE/ZLevels/` (86 cs).
3. [x] Copied CE `Content.Server/_CE/PVS/` (2 cs).
4. [x] Copied CE `_CE/ZCollapse/` (12 cs).
5. [x] **Server compiles (0 errors).** Two fixes in `CEZLevelToolTileSystem.cs`: CE's engine `PrototypeFlags.Overlaps` doesn't exist here → use `ContainsAny`; and to keep Read-only access to `ToolComponent` (analyzer RA0002) call it with the **prototype as receiver**: `tileDef.DeconstructTools.ContainsAny(tool.Qualities)`. (Root cause: we're behind on the engine upstream-merge — the fix survives a later upstream sync.)
6. [x] Integration points compiled clean against CE API (server+shared): `PrecipitationSystem.cs`, `ThrownItemSystem.cs`. `Content.Client/Parallax/ParallaxOverlay.cs` pending client build. Riding `RelativeRotation` fix is in upstream `SharedMoverController.cs` (outside `_CE`, untouched).
7. [x] Resources brought (system-level only — NOT CE's world content, which is entangled): actions `_CE/Entities/Actions/{zLevels,flight}.yml`, `_CE/Entities/zeye.yml`, extracted `_CE/Entities/Effects/zlevel_dust.yml` (CEDust*Effect, `ForkFiltered` category dropped — undefined here), `_CE/Shaders/zlevel.yml` (CEZBlur); textures `_CE/{Actions/misc.rsi, Effects/dust.rsi, Effects/TileEffects/dust.rsi, Shaders/zblur.swsl}`; locale `_CE/{zLevel,tiles,softPaws}` (ru+en). All z-level actions are granted **by code** (component `ActionId`/`*ActionProto` → `AddAction`), so players need no YAML grants. Deleted stale `_Respiral/Entities/Actions/zLevels.yml` (referenced the removed `CEToggleZLevelLookUpAction` event) + its orphan locale.
8. [ ] IN PROGRESS: `Content.YAMLLinter` (prototype/serialization/texture validation), then client build + `dotnet test Content.IntegrationTests`. CCVars partial targets upstream `Content.Shared.CCVar.CCVars` (safe).

### Follow-up owed
- **upstream-merge** planned by user right after this port (CE is on a newer engine — the `ContainsAny` shim is compatible either way).
- Check our maps/prototypes (`verdant_vale`, `edgehaven`, `empty`, `stairs`, walls, `observer`, mobs) still reference valid CE component ids after the port (linter/integration test will surface mismatches).

### Result (2026-09-05)
Server compiles **0 errors**. `Content.YAMLLinter` (builds client+server, loads all prototypes) reports
**24 errors, NONE from the migration** — all pre-existing & unrelated: species `voice` field non-nullable
(from the TTS commits), `_Respiral` wallpaper construction loc/`icon`, `_Respiral` medical stacks/texture.
Zero errors mention `_CE`/zlevels/actions/zeye/dust/blur ⇒ the ZLevels re-port (code + brought resources)
validates clean. Still worth running `dotnet test Content.IntegrationTests` for actual map load
(verdant_vale/edgehaven/empty) once the pre-existing 24 are triaged, and doing the planned upstream-merge.

### Integration test (2026-09-05)
`dotnet test --filter FullyQualifiedName~RespiralMapTest` → **9/9 passed** (RespiralMapsLoadableTest):
all Respiral maps (verdant_vale/edgehaven/empty × their z-levels) load cleanly with the ported CE
ZLevels components. Harness starts fine despite the 24 pre-existing lint errors ⇒ they're lint-level,
not load-blocking. Migration validated. (Full `dotnet test Content.IntegrationTests` would surface the
24 pre-existing failures — unrelated to ZLevels.)
