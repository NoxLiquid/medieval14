# Precipitation — weather-driven rain visuals

Respiral precipitation system (starting with rain). Builds on upstream Weather. Purely visual/audio:
it toggles an upstream weather effect (parallax + sound) on the map for a while. It does NOT wet
tiles or spill any reagent — puddle spawning was removed.

## Architecture

- **Prototype** `PrecipitationPrototype` — `Content.Shared/_Respiral/Precipitation/PrecipitationPrototype.cs`, `[Prototype]` → `type: precipitation`.
  - `weather` (`EntProtoId`): the upstream weather status effect that carries the parallax sprite + sound (e.g. `WeatherRain`). Parallax/sound are NOT stored on the precipitation prototype — `WeatherStatusEffectComponent` is `Access`-restricted to `SharedWeatherSystem`, so we reference a weather prototype instead of duplicating fields.
  - `duration` (`TimedWindow`): how long the precipitation lasts (random in [min,max]).
- **Component** `MapPrecipitationComponent` (server-only) — `Content.Server/_Respiral/Precipitation/Components/`. Put on a **map** entity to enable the auto-cycle.
  - `possible` (`List<ProtoId<PrecipitationPrototype>>`): pool to pick from.
  - `interval` (`TimedWindow`): dry period between precipitations.
  - Runtime: `Active` (current proto or null), `EndTime`.
- **System** `PrecipitationSystem` (server) — `Content.Server/_Respiral/Precipitation/`.
- **Command** `precip start <id>` / `precip stop` — `Content.Server/_Respiral/Commands/PrecipitationCommand.cs` (AdminFlags.Fun), acts on the player's current map. Z-network aware: `TryGetNetworkPrecipitation` reuses an existing `MapPrecipitationComponent` anywhere in the map's z-network (so `start` doesn't create a duplicate cycle, and `stop` finds a component living on another level); only falls back to the player's map if none exists.

## Flow (auto-cycle)

1. `MapInitEvent` on `MapPrecipitationComponent` → `TimedWindowSystem.Reset(Interval)` schedules the first rain.
2. Dry: when `Interval` expires and `Possible` is non-empty → `StartPrecipitation` with a random proto.
3. `StartPrecipitation`: computes a random duration from `proto.Duration`, calls `WeatherSystem.TrySetWeather(mapId, proto.Weather, duration)` (parallax + sound on), sets `EndTime`.
4. When `now >= EndTime`: `StopPrecipitation` → `WeatherSystem.TryRemoveWeather` (fades out) and `Reset(Interval)` for the next dry period.

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
- Without ZLevels (`CEZMapComponent` absent), behavior is single-map. Degrades gracefully.

Parallax masking is per-tile via `Content.Client/Overlays/StencilOverlay.Weather.cs` (stencils out tiles where `SharedWeatherSystem.CanWeatherAffect` is false), so indoors/roofed tiles never show rain even though the weather effect exists on the map. Coverage between z-levels is automatic: `CESharedZLevelsRoofSystem` propagates roof state down (a lower tile covered by a non-`Transparent` tile above gets `RoofComponent`), so `CanWeatherAffect` excludes covered lower tiles.

Which tiles show the parallax is gated by `ContentTileDefinition.Weather` (`weather: true` in the tile YAML) plus roof/`BlockWeather`. See `Resources/Prototypes/_Respiral/Tiles/floors.yml`.

## Notes / limitations

- Puddle/reagent spilling was removed. Precipitation is now visual-only; nothing wets tiles. If tile wetting is wanted again, it belongs in a separate system, not here.
- `WeatherSystem` (server) is depended on directly; `TrySetWeather`/`TryRemoveWeather` are public on `SharedWeatherSystem`.
- Weather has built-in 15s fade in/out (`StartupTime`/`ShutdownTime` in `SharedWeatherSystem`).
- `TimedWindow` (`Content.Shared/_Respiral/TimeWindow/`) is a shared DataDefinition; `GetRandomTime` does not mutate the window, `Reset` does. The prototype's `Duration` is read-only config (never `Reset`), the component's `Interval` is per-map runtime state.
