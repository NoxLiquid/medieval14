# Inventory — Slot System

## How Inventory Slots Work

1. **SlotFlags** (`Content.Shared/Inventory/SlotFlags.cs`) — bitfield enum defining slot types (HEAD, FEET, LEGS, etc.)
2. **InventoryTemplatePrototype** (`Content.Shared/Inventory/InventoryTemplatePrototype.cs`) — C# data definition for `SlotDefinition` (name, slotFlags, uiWindowPos, strippingWindowPos, displayName, stripTime, dependsOn, etc.)
3. **Inventory templates** (YAML) — `Resources/Prototypes/InventoryTemplates/human_inventory_template.yml` defines all slots for humanoids
4. **TemporarySlotMap** (`Content.Client/Clothing/ClientClothingSystem.cs`, ~line 30) — maps slot name to RSI state prefix (e.g. `"shoes"` → `"FEET"`, `"pants"` → `"LEGS"`)
5. **Slot textures** — UI icons at `Resources/Textures/Interface/{Theme}/Slots/{textureName}.png`; each theme (Default, Retro, Ashen, Minimalist, Clockwork, Slimecore, Plasmafire) has its own set

## Adding a New Slot — Checklist

- Add flag to `SlotFlags.cs` if needed (LEGS already exists)
- Add entry in `human_inventory_template.yml` with unique `uiWindowPos` and `strippingWindowPos`
- Add mapping in `TemporarySlotMap` in `ClientClothingSystem.cs`
- **Add a layer "bookmark" `- map: ["<slot>"]` in `Resources/Prototypes/Body/species_appearance.yml`** (prototype `BaseSpeciesLayers`) at the desired draw depth — ОБЯЗАТЕЛЬНО, иначе одежда этого слота рисуется поверх всего (см. ниже)
- Create slot icon PNG in each theme's `Slots/` directory (or reuse existing texture name)
- Clothing items use `equipped-{STATE_PREFIX}` RSI state (e.g. `equipped-LEGS`)

## Draw order одежды на персонаже (КРИТИЧНО)

Глубину отрисовки надетой одежды задаёт ПОРЯДОК слоёв-закладок в `Body/species_appearance.yml`
(`BaseSpeciesLayers` → `Sprite.layers`, каждый `- map: ["<slotName>"]`). Раньше в списке = ниже.

`ClientClothingSystem.RenderEquipment`: для слота ищется слой-закладка с ключом = имя слота.
- **закладка есть** → слой одежды вставляется сразу после неё (фиксированная глубина);
- **закладки НЕТ** → `LayerMapReserve` добавляет слой в КОНЕЦ списка → одежда рисуется **поверх всего**
  (рук, брони, головы). Это типичный баг при добавлении нового слота.

Пример (исправленный баг слота `pants`): без `- map: ["pants"]` штаны рисовались поверх рубахи,
кистей рук и `outerClothing`. Закладка добавлена **перед** `jumpsuit` (между бельём и нательной
одеждой) → рубаха/туника рисуется поверх штанов, а сами штаны — под кистями рук, обувью и бронёй.
Положение pants ОТНОСИТЕЛЬНО jumpsuit определяет, что поверх чего: `pants` перед `jumpsuit` =
рубаха поверх штанов; после = штаны поверх рубахи. `uiWindowPos`/`TemporarySlotMap` на это НЕ
влияют — только порядок в `species_appearance.yml`.

## Current Respiral Additions

- **Pants slot** added: uses `LEGS` flag, `TemporarySlotMap` entry `"pants"` → `"LEGS"`, placed at `uiWindowPos: 0,0` / `strippingWindowPos: 0,3`
- **Sex-specific clothing visuals** in `ClientClothingSystem.OnGetVisuals`: lookup chain is `{slot}-{species}-{sex}` → `{slot}-{species}` → `{slot}-{sex}` → `{slot}` → default RSI

## UI Grid Positions (human template, default group)

| Slot | uiWindowPos | strippingWindowPos |
|---|---|---|
| pants | 0,0 | 0,3 |
| shoes | 1,0 | 1,3 |
| suitstorage | 2,0 | 2,5 |
| jumpsuit | 0,1 | 0,2 |
| outerClothing | 1,1 | 1,2 |
| gloves | 2,1 | 2,2 |
| neck | 0,2 | 0,1 |
| mask | 1,2 | 1,1 |
| ears | 2,2 | 2,0 |
| eyes | 0,3 | 0,0 |
| head | 1,3 | 1,0 |

## Bottom hotbar layout (slotGroup)

Слоты с `slotGroup` (НЕ `null`) не попадают в окно-куклу экипировки, а рендерятся в нижнем
хотбаре. Контейнеры заданы в `Content.Client/.../Hotbar/Widgets/HotbarGui.xaml`:
два `ItemSlotButtonContainer` — `MainHotbar` и `SecondHotbar` — стоят по бокам от рук.

**Порядок кнопок внутри группы = порядок определения слотов в шаблоне инвентаря** (не `uiWindowPos`!).
Рендер: `InventoryUIController.AddSlot` → `container.TryAddButton` → `AddChild` в порядке итерации
`SlotData` (= порядок слотов в YAML). `uiWindowPos`/`strippingWindowPos` для сгруппированных слотов
на хотбар-раскладку **не влияют** (используются только окном раздевания и тогл-баром экипировки).

Текущая раскладка (Respiral):
- `MainHotbar`: `pocket1, pocket2, id, back` — id/PDA и рюкзак стоят сразу после карманов.
- `SecondHotbar`: `belt, suitstorage`.

Чтобы переставить слот в хотбаре — менять его `slotGroup` и/или его позицию среди определений
слотов в `human_inventory_template.yml`, а не `uiWindowPos`.

## Known Gaps

- Pants slot uses `uniform` placeholder texture — needs dedicated `pants.png` icons for all UI themes
- Базовые прототипы и контент одежды теперь есть — см. `clothing.md`
