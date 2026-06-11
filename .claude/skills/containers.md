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

### Solutions Refactor (апстрим #43412) — новая модель растворов

Апстрим перешёл с `SolutionContainerManager.solutions.<id>` на компонент-на-раствор
`Solution`. Базы еды/питья теперь сами дают раствор нужного id:
- `SolutionFood` → `- type: Solution` с `id: food`; входит в `FoodBase`/`FoodProduceBase`.
- `SolutionDrink` → `- type: Solution` с `id: drink`; входит в `DrinkBase`.

**Симптом конфликта:** если сущность наследует такую базу И при этом задаёт старый
`SolutionContainerManager.solutions.food/drink`, движок при портировании логирует
`[WARN] system.solution_container: Attempted to port a solution id: food ... but the
entity already had a solution with that id`. В пуле интеграционных тестов WARN = падение.

**Фикс — заменить старый блок на новый компонент `Solution` (на один уровень меньше вложенности):**

```yaml
# было (легаси):
- type: SolutionContainerManager
  solutions:
    food:
      maxVol: 10
      reagents:
      - ReagentId: JuiceCarrot
        Quantity: 4
# стало (апстримный паттерн, ср. FoodCarrot):
- type: Solution
  solution:
    maxVol: 10
    reagents:
    - ReagentId: JuiceCarrot
      Quantity: 4
```

`id` (`food`/`drink`) подтягивается из базы; `maxVol`/`reagents` мёржатся в неё.
Для нескольких растворов на одной сущности — несколько компонентов `Solution` с разными `id`.

**Когда НЕ трогать:** `SolutionContainerManager` остаётся корректным, если сущность
сама определяет собственный раствор и НЕ наследует базу с таким же `Solution.id`
(бочки/структуры в `barrel.yml`, `alchem_table.yml`, `soil.yml`, `production.yml`,
`cow.yml`, `goat.yml` — конфликта нет, их не конвертировали).

Конвертированы (упомянутые в варнингах middle-food/drink): `herb.yml`, `drinks.yml`
(2 базы — `MedievalDrinkBaseCup`, `MedievalDrinkBaseBottle` — чинят всех детей-кружек/бутылок),
`Consumable/Food/produce.yml`, `RawMaterials/Seeds/produce.yml`.

### Файлы
| Путь | Содержимое |
|---|---|
| `Content.Shared/_Respiral/Barrel/BarrelComponent.cs` | Маркер-компонент `[NetworkedComponent]` |
| `Content.Shared/_Respiral/Barrel/BarrelSystem.cs` | Блокировка `SolutionTransferAttemptEvent` при закрытом хранилище |
| `Content.Client/_Respiral/Barrel/BarrelVisualizerSystem.cs` | Скрытие Fill-слоя при закрытом хранилище |
| `Resources/Prototypes/_Respiral/Entities/Structures/Furniture/barrel.yml` | Прототип `MedievalBarrel` |
| `Resources/Locale/ru-RU/_Respiral/barrel.ftl` | Локализация popup |
