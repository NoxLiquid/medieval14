# Precipitation — weather-driven rain and liquid on tiles

Respiral precipitation system (starting with rain). Builds on upstream Weather and on the tile
absorption system (see [[fluids]]).

## Architecture

- **Prototype** `PrecipitationPrototype` — `Content.Shared/_Respiral/Precipitation/PrecipitationPrototype.cs`, `[Prototype]` → `type: precipitation`.
  - `weather` (`EntProtoId`): the upstream weather status effect that carries the parallax sprite + sound (e.g. `WeatherRain`). Parallax/sound are NOT stored on the precipitation prototype — `WeatherStatusEffectComponent` is `Access`-restricted to `SharedWeatherSystem`, so we reference a weather prototype instead of duplicating fields.
  - `reagent` (`ProtoId<ReagentPrototype>`): what gets spilled (e.g. `Water`).
  - `puddleAmount` (FixedPoint2): units spilled per wetted tile.
  - `tilesPerTick` (int): random tiles wetted per spill tick.
  - `spillInterval` (TimeSpan): delay between spill ticks.
  - `duration` (`TimedWindow`): how long the precipitation lasts (random in [min,max]).
- **Component** `MapPrecipitationComponent` (server-only) — `Content.Server/_Respiral/Precipitation/Components/`. Put on a **map** entity to enable the auto-cycle.
  - `possible` (`List<ProtoId<PrecipitationPrototype>>`): pool to pick from.
  - `interval` (`TimedWindow`): dry period between precipitations.
  - Runtime: `Active` (current proto or null), `EndTime`, `NextSpill`.
- **System** `PrecipitationSystem` (server) — `Content.Server/_Respiral/Precipitation/`.
- **Command** `precip start <id>` / `precip stop` — `Content.Server/_Respiral/Commands/PrecipitationCommand.cs` (AdminFlags.Fun), acts on the player's current map. Z-network aware: `TryGetNetworkPrecipitation` reuses an existing `MapPrecipitationComponent` anywhere in the map's z-network (so `start` doesn't create a duplicate cycle, and `stop` finds a component living on another level); only falls back to the player's map if none exists.

## Flow (auto-cycle)

1. `MapInitEvent` on `MapPrecipitationComponent` → `TimedWindowSystem.Reset(Interval)` schedules the first rain.
2. Dry: when `Interval` expires and `Possible` is non-empty → `StartPrecipitation` with a random proto.
3. `StartPrecipitation`: computes a random duration from `proto.Duration`, calls `WeatherSystem.TrySetWeather(mapId, proto.Weather, duration)` (parallax + sound on), sets `EndTime` and `NextSpill`.
4. Each `SpillInterval`: `SpillTick` wets `TilesPerTick` random eligible tiles via `PuddleSystem.TrySpillAt(tileRef, new Solution(reagent, amount), sound: false)`.
5. When `now >= EndTime`: `StopPrecipitation` → `WeatherSystem.TryRemoveWeather` (fades out) and `Reset(Interval)` for the next dry period.

## Tile selection

`TryPickExposedTile`: pick a random grid on the map (grids collected via `MapGridComponent + TransformComponent` filtered by `MapUid`), a random `Vector2i` inside `grid.LocalAABB`, then require:
- not `tile.IsEmpty`;
- `SharedWeatherSystem.CanWeatherAffect((grid, roof?), tileRef)` — open to the sky (respects roof / `ContentTileDefinition.Weather` / block entities);
- `!ContentTileDefinition.Absorbent` — absorbent tiles (dirt/sand) are skipped per design.

Up to 5 attempts per requested tile; misses are skipped silently.

## Enabling on a map

Add the component to a map prototype (or via `precip` for testing):

```yaml
- type: MapPrecipitation
  possible:
  - Rain
  interval:
    min: 3m
    max: 8m
```

## ZLevels integration

Precipitation is z-network aware (`_CE/ZLevels`). If the map holding `MapPrecipitationComponent` has a `CEZMapComponent`, the system operates over the **whole z-network**:

- **Weather (parallax + sound)** is set on **every** map of the network (loop over maps calling `WeatherSystem.TrySetWeather`), so players on any level see/hear it. A single component drives the whole network — put it on **one** map only (top recommended); `GetPrecipitationMaps` covers self + `CESharedZLevelsSystem.GetAllMapsAbove` + `GetAllMapsBelow`.
- **Spills** are picked across grids of **all** network maps (`CollectGrids` filters grids by `MapUid ∈ network maps`).
- **Coverage between levels is automatic.** `CESharedZLevelsRoofSystem` already propagates roof state down: a lower tile covered by a non-`Transparent`/rooved tile above gets `RoofComponent` roof data. So `CanWeatherAffect` (used both by our tile picker and by the client stencil overlay) excludes covered lower tiles — no manual "is this tile covered from above" check is needed, and rain doesn't fall through solid upper floors.
- Without ZLevels (`CEZMapComponent` absent), behavior is single-map. Degrades gracefully.

Parallax masking itself is per-tile via `Content.Client/Overlays/StencilOverlay.Weather.cs` (stencils out tiles where `CanWeatherAffect` is false), so indoors/roofed tiles never show rain even though the weather effect exists on the map.

## Notes / limitations

- `WeatherSystem` (server) is depended on directly; `TrySetWeather`/`TryRemoveWeather`/`CanWeatherAffect` are public on `SharedWeatherSystem`.
- Weather has built-in 15s fade in/out (`StartupTime`/`ShutdownTime` in `SharedWeatherSystem`); spills run for the full duration regardless.
- `TimedWindow` (`Content.Shared/_Respiral/TimeWindow/`) is a shared DataDefinition; `GetRandomTime` does not mutate the window, `Reset` does. The prototype's `Duration` is read-only config (never `Reset`), the component's `Interval` is per-map runtime state.
- Grids are re-collected each spill tick (cheap: few grids, 1 tick/sec). Cache if it ever matters.
