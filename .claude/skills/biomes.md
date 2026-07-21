# Biome System (Procedural map generation)

## Architecture

### Core components

| Class | Purpose |
|---|---|
| `BiomeComponent` | Component on the map entity. Holds seed, layers, loaded chunks |
| `BiomeTemplatePrototype` | YAML prototype: list of `IBiomeLayer` — biome definition |
| `BiomeSystem` (Server) | Loads/unloads chunks based on proximity to players |
| `SharedBiomeSystem` | Logic for determining tiles/entities/decals from noise |

### Layer types (`IBiomeLayer`)

| Type | What it does |
|---|---|
| `BiomeTileLayer` | Generates tiles from noise (`Tile`, `Threshold`, `Noise`) |
| `BiomeEntityLayer` | Spawns entities on allowed tiles (`Entities`, `AllowedTiles`) |
| `BiomeDecalLayer` | Places decals (`Decals`, `AllowedTiles`, `Divisions`) |
| `BiomeMetaLayer` | Nested biome: delegates to another `BiomeTemplatePrototype` |
| `BiomeDummyLayer` | Placeholder marker for injecting layers from code |

### Each layer has

- `Noise` — `FastNoiseLite` (seed, noiseType, frequency, fractalType, octaves...)
- `Threshold` — noise value above which the layer is active
- `Invert` — invert the noise

### Processing order

Layers are processed **bottom-up** (from last to first). The first matching layer wins. In YAML: lower layers are base tiles, upper layers are decals/entities.

## Generation process

1. `BiomeSystem.Update()` — each tick finds player positions
2. Computes active chunks (within `_loadRange` of players, ChunkSize=8)
3. `LoadChunk()` — for each 8×8 tile, iterates layers, places tiles → entities → decals
4. `UnloadChunk()` — removes unmodified tiles/entities/decals when the player moves away
5. `ModifiedTiles` — tiles changed by players are never overwritten

## Markers (BiomeMarkerLayerPrototype)

A separate system for spawning groups of entities (ore, mobs):
- `Size` — marker chunk size (usually 128)
- `Radius` — minimum distance between groups
- `MaxCount` — maximum number of groups
- `MinGroupSize`/`MaxGroupSize` — group size
- `EntityMask` — replacement of existing entities (e.g. wall → ore)
- `Prototype` — what to spawn

## Creating a planet

`BiomeSystem.EnsurePlanet()` — creates a map with a biome:
- Adds `BiomeComponent` with template and seed
- Gravity, MapLight, LightCycle, Roof, SunShadow, atmosphere

## Gateway (portal-planet generation)

`GatewayGeneratorSystem.GenerateDestination()`:
1. Creates a new map
2. Calls `EnsurePlanet()` with a biome
3. Adds marker layers (ore, mobs)
4. When the portal opens — generates a dungeon via `DungeonSystem`

## Prototype files

| File | Contents |
|---|---|
| `Prototypes/Procedural/biome_templates.yml` | Upstream biomes (Continental, Lava, Snow...) |
| `Prototypes/_Respiral/Procedural/biome_templates.yml` | `MedievalGrasslands` — medieval biome |

## System limitations

- **No support for coordinate constraints** — no `IBiomeLayer` has bounds/region/area fields
- A biome applies to the **whole map** infinitely
- Generation is determined solely by **noise functions** and **thresholds**
- `BiomeMetaLayer` allows nested biomes, but still via noise, not coordinates

## BiomeRegionLayer (Respiral)

A custom `IBiomeLayer` for coordinate-constrained nested biomes.

### Files

| File | Purpose |
|---|---|
| `Content.Shared/_Respiral/Parallax/Biomes/Layers/BiomeRegionLayer.cs` | Layer type + `BiomeRegionShape` enum |
| `Content.Shared/Parallax/Biomes/SharedBiomeSystem.cs` | Handling in TryGetTile/TryGetEntity/TryGetDecals (`// Respiral` comments) |

### Shapes

- `Rectangle` — defined by `origin` (bottom-left) and `end` (top-right, exclusive)
- `Circle` — defined by `center` and `radius`

### Priority

Determined by position in the biome template's `layers` list. Layers later in the list have higher priority (processed first due to reverse iteration).

### YAML example

```yaml
- type: biomeTemplate
  id: MyWorldBiome
  layers:
    # Base tiles (lowest priority)
    - !type:BiomeTileLayer
      threshold: -1.0
      tile: MedievalFloorGrass

    # Forest zone 100×100 tiles
    - !type:BiomeRegionLayer
      template: ForestBiome
      shape: Rectangle
      origin: 0, 0
      end: 100, 100

    # Lake inside the forest (higher priority — drawn on top)
    - !type:BiomeRegionLayer
      template: LakeBiome
      shape: Circle
      center: 50, 50
      radius: 15
```
