# Construction System

## File Layout

| Path | Purpose |
|---|---|
| `Resources/Prototypes/_Respiral/Construction/*.yml` | `construction` recipes (UI entries, link graph→node) |
| `Resources/Prototypes/_Respiral/Construction/Graph/Structure/*.yml` | `constructionGraph` definitions (nodes + edges + steps) |
| `Resources/Prototypes/_Respiral/Entities/Structures/**/*.yml` | Entity prototypes (must have `Construction` component pointing to graph+node) |

## Construction Recipe Pattern

```yaml
- type: construction
  id: MedievalFoo
  graph: MedievalFooGraph      # constructionGraph id
  startNode: start
  targetNode: fooNode           # target node in that graph
  category: construction-category-structures   # or construction-category-furniture
  objectType: Structure
  placementMode: SnapgridCenter
  canRotate: false
  canBuildInImpassable: false
  conditions:
    - !type:TileNotBlocked
```

## Construction Graph Pattern

Two variants:

### Direct (no frame intermediate — fences, roofs, simple furniture)
```yaml
- type: constructionGraph
  id: MedievalFoo
  start: start
  graph:
  - node: start
    edges:
    - to: result
      completed:
        - !type:SnapToGrid
          southRotation: true
      steps:
        - material: MedievalBoard
          amount: 2
          doAfter: 2
  - node: result
    entity: MedievalFooEntity
```

### With Frame intermediate (walls, doors — heavier structures)
```yaml
- node: start → wallFrame (place material) → wall (add more material)
```
Frame entity must exist separately as a girder/frame prototype.

## Materials Used in Construction

| ID | Use |
|---|---|
| `MedievalBoard` | Wood planks (from MedievalProcessedWood) |
| `MedievalProcessedStone` | Cut stone blocks |
| `MedievalConstructionToolsAmmo` | Nails/pegs — required as first step for most structures |
| `MedievalCloth` | Fabric (beds, curtains) |
| `MedievalProcessedPelt` | Tanned leather |

## Destruction Balance (Destructible thresholds)

Damage container: `MedievalInorganic` for all structures.

| Structure type | Modifier set | Destroy HP |
|---|---|---|
| Furniture (small) | MedievalWood | 300 |
| Roof (wooden) | MedievalWood | 600 |
| Roof (birch) | MedievalWood | 700 |
| Roof (stone) | MedievalStone | 1200 |
| Wall (wooden) | MedievalWood | 2000 (crumbles to frame at 1000) |
| Wall (birch) | MedievalWood | 2100 (crumbles at 1050) |

Sound collections on destroy:
- `WoodDestroy` — light structures (fences, small furniture)
- `WoodDestroyHeavy` — heavy structures (walls, doors, roofs)
- No stone-specific sound collection exists yet; use `WoodDestroyHeavy` for stone structures.

## Stairs (ZLevel лестницы)

Сущности в `Resources/Prototypes/_Respiral/ZLevels/stairs.yml`.
Граф крафта: `MedievalLadder` (`Construction/Graph/Structure/stairs.yml`).
Рецепты: `Construction/other.yml`.

| ID | Спрайт state | color | Материал крафта |
|---|---|---|---|
| `MedievalCEZLevelLadder` | `stairs_wood` | — (без color) | MedievalBoard ×2 |
| `MedievalCEZLevelLadderStone` | `stairs_stone` | `#808080` | MedievalProcessedStone ×2 |
| `MedievalCEZLevelLadderMarble` | `stairs_stone` | `#E8E0C8` | MedievalProcessedStone ×3 |

- Спрайты нейтрального цвета — цвет материала задаётся через `color` в компоненте `Sprite`.
- Мрамор использует тот же спрайт `stairs_stone`, что и камень, но с ivory-цветом `#E8E0C8`.
- Мрамор дороже в постройке (×3 камня) и демонтаже.
- Цвета берутся из `materials.yml`: `MedievalProcessedStone → #808080`.

## Roof System

### Entities (`Resources/Prototypes/_Respiral/Entities/Structures/Roof/roof.yml`)

| ID | Sprite RSI | Material |
|---|---|---|
| `MedievalRoofWooden` | `_Respiral/Structures/Roof/wooden.rsi` | MedievalBoard |
| `MedievalRoofBirch` | `_Respiral/Structures/Roof/birch.rsi` | MedievalBoard |
| `MedievalRoofStone` | `_Respiral/Structures/Roof/stone.rsi` | MedievalProcessedStone |

All inherit `MedievalBaseRoof` (abstract). Base uses `test.rsi` as fallback placeholder.

RSI sprite states: `roof0`–`roof7` (4 directions each) + `full` (icon).
`IconSmooth` key: `MedievalRoof`, base: `roof`.

Construction graph: `MedievalRoof` (`Construction/Graph/Structure/roof.yml`).
Construction recipes: `Construction/roof.yml`.
