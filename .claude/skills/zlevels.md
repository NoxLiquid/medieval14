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

## Known issues and solutions

### `Required properties not provided for` in Serilog logs
**Cause:** `StationNameSetup.mapNameTemplate` contains placeholders `{0}`, `{1}` without setting a `nameGenerator`. The station name stays unformatted. When `StationSystem.AddGridToStation` logs (line 350), Serilog interprets `{0}` and `{1}` as named template parameters.
**Solution:** Either remove the placeholders from `mapNameTemplate`, or add a `nameGenerator` to the prototype.
