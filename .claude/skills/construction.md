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

## Key/Lock System

**Компоненты:**
- `MedievalKeyComponent.KeyId` — строка-идентификатор ключа
- `MedievalLockComponent.KeyId` — требуемый ключ; `Locked` — заблокирован ли замок
- `MedievalLockableComponent` — на двери/сундуке: разрешает блокировку
- Замок ищется через `lock-container` (`ContainerSlot`) на двери, ИЛИ через `MedievalLockComponent` прямо на двери

**Файлы прототипов:**
| Файл | Содержимое |
|---|---|
| `Entities/Items/Misk/key.yml` | Базовые прототипы (`MedievalBaseKey`, `MedievalKey`, отмычки) |
| `Entities/Items/Misk/key_sets.yml` | 37 ключей по keyId |
| `Entities/Items/Misk/lock.yml` | Базовые прототипы замков |
| `Entities/Items/Misk/lock_sets.yml` | 37 замков по keyId (`locked: true`) |
| `Entities/Structures/Doors/locked_door.yml` | 222 двери = 6 типов × 37 keyId |

**Именование:** `MedievalKey{Latin}` / `MedievalLock{Latin}` / `MedievalWoodenDoor{Latin}`, suffix = кириллический keyId.

**Список keyId:** кузнец, портной, алхимик, плотник, каменщик, торговец, таверна, таверна1–6, лорд, казарма, тюрьма, староста, амбар, склад, казна, стража, дом1–10, управляющий, рыцарь1–5.

**Locked door:** наследует тип двери + `ContainerContainer` с пустым `lock-container: !type:ContainerSlot`. Замок (`MedievalLockKuznec` и т.д.) кладётся в слот на карте, НЕ через прототип (ContainerSlot.ent — EntityUid, не прото-ID). Имя компонента в YAML: `ContainerContainer` (т.к. `[ComponentProtoName("ContainerContainer")]`). `SharedMedievalLockSystem` обращается только к замку внутри контейнера через `GetLock()`, `MedievalLock` на двери не нужен.

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
