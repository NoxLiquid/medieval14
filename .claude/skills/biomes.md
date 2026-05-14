# Biome System (Процедурная генерация карт)

## Архитектура

### Основные компоненты

| Класс | Назначение |
|---|---|
| `BiomeComponent` | Компонент на map-entity. Содержит seed, layers, загруженные чанки |
| `BiomeTemplatePrototype` | YAML-прототип: список `IBiomeLayer` — определение биома |
| `BiomeSystem` (Server) | Загрузка/выгрузка чанков по proximity к игрокам |
| `SharedBiomeSystem` | Логика определения тайлов/сущностей/декалей по noise |

### Типы слоёв (`IBiomeLayer`)

| Тип | Что делает |
|---|---|
| `BiomeTileLayer` | Генерирует тайлы по noise (`Tile`, `Threshold`, `Noise`) |
| `BiomeEntityLayer` | Спавнит сущности на разрешённых тайлах (`Entities`, `AllowedTiles`) |
| `BiomeDecalLayer` | Размещает декали (`Decals`, `AllowedTiles`, `Divisions`) |
| `BiomeMetaLayer` | Вложенный биом: делегирует к другому `BiomeTemplatePrototype` |
| `BiomeDummyLayer` | Маркер-заглушка для инъекции слоёв из кода |

### Каждый слой имеет

- `Noise` — `FastNoiseLite` (seed, noiseType, frequency, fractalType, octaves...)
- `Threshold` — значение noise, выше которого слой активен
- `Invert` — инвертировать noise

### Порядок обработки

Слои обрабатываются **снизу вверх** (от последнего к первому). Первый совпавший слой побеждает. В YAML: нижние слои — базовые тайлы, верхние — декали/сущности.

## Процесс генерации

1. `BiomeSystem.Update()` — каждый тик находит позиции игроков
2. Вычисляет активные чанки (в радиусе `_loadRange` от игроков, ChunkSize=8)
3. `LoadChunk()` — для каждого тайла 8×8 проходит по layers, ставит тайлы → сущности → декали
4. `UnloadChunk()` — удаляет неизменённые тайлы/сущности/декали при удалении от игрока
5. `ModifiedTiles` — тайлы, изменённые игроками, никогда не перезаписываются

## Маркеры (BiomeMarkerLayerPrototype)

Отдельная система для спавна групп сущностей (руды, мобы):
- `Size` — размер чанка маркера (обычно 128)
- `Radius` — минимальное расстояние между группами
- `MaxCount` — максимум групп
- `MinGroupSize`/`MaxGroupSize` — размер группы
- `EntityMask` — замена существующих сущностей (напр. стена → руда)
- `Prototype` — что спавнить

## Создание планеты

`BiomeSystem.EnsurePlanet()` — создаёт карту с биомом:
- Добавляет `BiomeComponent` с template и seed
- Gravity, MapLight, LightCycle, Roof, SunShadow, атмосфера

## Gateway (генерация планет-порталов)

`GatewayGeneratorSystem.GenerateDestination()`:
1. Создаёт новую карту
2. Вызывает `EnsurePlanet()` с биомом
3. Добавляет маркерные слои (руда, мобы)
4. При открытии портала — генерирует данжен через `DungeonSystem`

## Файлы прототипов

| Файл | Содержимое |
|---|---|
| `Prototypes/Procedural/biome_templates.yml` | Апстрим биомы (Continental, Lava, Snow...) |
| `Prototypes/_Respiral/Procedural/biome_templates.yml` | `MedievalGrasslands` — средневековый биом |

## Ограничения системы

- **Нет поддержки координатных ограничений** — ни один `IBiomeLayer` не имеет полей bounds/region/area
- Биом применяется ко **всей карте** бесконечно
- Генерация определяется исключительно **noise-функциями** и **threshold**
- `BiomeMetaLayer` позволяет вложенные биомы, но всё равно через noise, не координаты

## BiomeRegionLayer (Respiral)

Кастомный `IBiomeLayer` для координатно-ограниченных вложенных биомов.

### Файлы

| Файл | Назначение |
|---|---|
| `Content.Shared/_Respiral/Parallax/Biomes/Layers/BiomeRegionLayer.cs` | Тип слоя + enum `BiomeRegionShape` |
| `Content.Shared/Parallax/Biomes/SharedBiomeSystem.cs` | Обработка в TryGetTile/TryGetEntity/TryGetDecals (комментарии `// Respiral`) |

### Формы

- `Rectangle` — задаётся через `origin` (левый нижний) и `end` (правый верхний, не включительно)
- `Circle` — задаётся через `center` и `radius`

### Приоритет

Определяется позицией в списке `layers` шаблона биома. Слои позже в списке имеют более высокий приоритет (обрабатываются первыми из-за обратной итерации).

### Пример YAML

```yaml
- type: biomeTemplate
  id: MyWorldBiome
  layers:
    # Базовые тайлы (низший приоритет)
    - !type:BiomeTileLayer
      threshold: -1.0
      tile: MedievalFloorGrass

    # Лесная зона 100×100 тайлов
    - !type:BiomeRegionLayer
      template: ForestBiome
      shape: Rectangle
      origin: 0, 0
      end: 100, 100

    # Озеро внутри леса (выше приоритет — рисуется поверх)
    - !type:BiomeRegionLayer
      template: LakeBiome
      shape: Circle
      center: 50, 50
      radius: 15
```
