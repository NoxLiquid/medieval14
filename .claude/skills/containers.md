# Containers & Barrels

## EntityStorage (Crates, Barrels, Lockers)

`EntityStorageComponent` — хранение сущностей внутри (не UI-инвентарь, а физическое исчезновение внутри).

### Ключевые поля
| Поле | Умолчание | Описание |
|---|---|---|
| `airtight` | `true` | Герметичность: если `true` — запирает воздух, человек задохнётся |
| `open` | `false` | Состояние открыт/закрыт |
| `capacity` | 30 | Макс. кол-во предметов |

### Визуал открытия/закрытия (EntityStorageVisuals)

**Критически важно:** `EntityStorageVisualizerSystem` проверяет наличие слоя `StorageVisualLayers.Door` в спрайте. Если слой отсутствует — весь блок визуализации пропускается, спрайт не меняется.

Два подхода:
1. **Base + Door** (как у MedievalBaseCrate): Base — корпус, Door — крышка/дверца
2. **Base swap** (как у бочки): Door присутствует формально (hidden), Base меняется через `stateBaseClosed`/`stateBaseOpen`

```yaml
# Паттерн сундуков (MedievalBaseCrate):
layers:
- state: base
  map: ["enum.StorageVisualLayers.Base"]
- state: closed
  map: ["enum.StorageVisualLayers.Door"]
# EntityStorageVisuals:
stateDoorOpen: open
stateDoorClosed: closed

# Паттерн бочки (MedievalBarrel):
layers:
- state: icon
  map: ["enum.StorageVisualLayers.Base"]
- state: icon
  map: ["enum.StorageVisualLayers.Door"]
  visible: false
# EntityStorageVisuals:
stateBaseClosed: icon
stateBaseOpen: barrel_open
```

### Коллизия
Сундуки используют `CrateMask`, не `MachineMask`.

## Barrel — гибрид EntityStorage + SolutionContainer

`BarrelComponent` (маркер) — `Content.Shared/_Respiral/Barrel/`.

### Требования
1. Хранение предметов (EntityStorage)
2. Хранение жидкости (SolutionContainer: `drink`, 500u)
3. Закрытая бочка: нельзя наливать/выливать жидкость, слой заполнения скрыт
4. Открытая бочка: можно наливать/выливать, слой виден
5. `airtight: false` — человек внутри не задохнётся

### Реализация блокировки переливания

`BarrelSystem` (Shared) подписывается на `SolutionTransferAttemptEvent` — отменяет трансфер если `EntityStorageComponent.Open == false`.

### Реализация скрытия Fill-слоя

**Проблема порядка визуализаторов:**
- `SolutionContainerVisualsSystem` безусловно ставит `Fill` → `visible: true` при наличии жидкости
- Если наш визуализатор отработает раньше — `SolutionContainerVisualsSystem` перетрёт скрытие

**Решение:** `BarrelVisualizerSystem` (Client, `EntitySystem`, НЕ `VisualizerSystem<T>`) подписывается на `AppearanceChangeEvent` с `after: [typeof(SolutionContainerVisualsSystem)]`.

**Почему не `VisualizerSystem<T>`:** базовый `Initialize()` подписывает обработчик без параметра `after:`. Пропуск `base.Initialize()` нарушает `RA0028`. Единственный корректный вариант — `EntitySystem` с ручными `[Dependency]` и явным ordering.

### Паттерн SolutionContainer на структурах

```yaml
- type: SolutionContainerManager
  solutions:
    drink:
      maxVol: 500
- type: SolutionContainerVisuals
  maxFillLevels: 5
  fillBaseName: liq       # спрайты: liq1, liq2, liq3, liq4, liq5
  changeColor: true
- type: RefillableSolution
  solution: drink
- type: DrainableSolution
  solution: drink
- type: ExaminableSolution
  solution: drink
- type: SolutionTransfer
  canChangeTransferAmount: true
  maxTransferAmount: 50
```

Для вычерпывания из бочки: предмет в руке должен иметь `SolutionTransferComponent` + `RefillableSolutionComponent` (кубки и кружки имеют через `DrinkBaseCup`).

### Файлы
| Путь | Содержимое |
|---|---|
| `Content.Shared/_Respiral/Barrel/BarrelComponent.cs` | Маркер-компонент `[NetworkedComponent]` |
| `Content.Shared/_Respiral/Barrel/BarrelSystem.cs` | Блокировка `SolutionTransferAttemptEvent` при закрытом хранилище |
| `Content.Client/_Respiral/Barrel/BarrelVisualizerSystem.cs` | Скрытие Fill-слоя при закрытом хранилище |
| `Resources/Prototypes/_Respiral/Entities/Structures/Furniture/barrel.yml` | Прототип `MedievalBarrel` |
| `Resources/Locale/ru-RU/_Respiral/barrel.ftl` | Локализация popup |
