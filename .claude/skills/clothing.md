# Clothing & Starting Gear

Domain: одежда (Respiral medieval clothing), её прототипы, RSI-спрайты и стартовое снаряжение профессий.

## Где что лежит

- **Спрайты:** `Resources/Textures/_Respiral/Clothing/<Slot>/*.rsi`
  - `OuterClothing/armor/` — внешняя броня (outerClothing)
  - `Legs/` — штаны/поножи (pants/legs slot)
  - `Shoes/` — обувь (feet)
  - `Head/Hats/`, `Head/Helmets/` — головные уборы (head)
  - `Neck/` — плащи/ожерелья (neck)
  - `Hands/` — перчатки (gloves)
  - `Uniforms/Jumpsuit/` — нательная одежда/рубахи (innerclothing/jumpsuit)
- **Прототипы:** `Resources/Prototypes/_Respiral/Entities/Clothing/`
  - `base.yml` — абстрактные базы `MedievalBase*` (Outer, Inner, Head, Neck, Gloves, Pants, Shoes, …)
  - `outer/armor.yml`, `legs.yml`, `shoes.yml`, `head.yml`, `neck.yml`, `gloves.yml`, `inner.yml`
- **Стартовое снаряжение:** `Resources/Prototypes/_Respiral/Roles/Jobs/*.yml` (`type: startingGear`)

## Базовые прототипы и их состояния спрайтов

Каждый `MedievalBase*` задаёт `Clothing.clothingVisuals` с фиксированным именем state.
**Имена PNG в .rsi должны совпадать с этими state**, иначе спрайт не отрисуется/движок упадёт.

| Слот | Базовый прототип | Ключ equipment | Ожидаемые state |
|---|---|---|---|
| innerclothing | `MedievalBaseInnerClothing` | `jumpsuit` | `equipped-INNERCLOTHING` |
| outerClothing | `MedievalBaseOuterClothing` | `outerClothing` | `equipped-OUTERCLOTHING` |
| head | `MedievalBaseHead` | `head` | `equipped-HELMET` |
| neck | `MedievalBaseNeck` | `neck` | `equipped-NECK` |
| gloves | `MedievalBaseGloves` | `gloves` | `equipped-HAND` |
| legs | `MedievalBasePants` | `pants` | `equipped-LEGS`, `equipped-LEGS-female` |
| feet | `MedievalBaseShoes` | `shoes` | `equipped-FEET`, `equipped-FEET-female` |

Все .rsi также содержат `icon`, `inhand-left`, `inhand-right`.
Ключи слотов equipment берутся из `Resources/Prototypes/InventoryTemplates/human_inventory_template.yml`
(слот штанов называется `pants`, нательки — `jumpsuit`).

## Материал → модификатор урона / состав

| Материал (из `База.txt`) | `damageModifierSet` | `PhysicalComposition` |
|---|---|---|
| Железо (Iron) | `MedievalIron` | `MedievalProcessedIron: 100` |
| Сталь (Steel) | `MedievalSteel` | `MedievalSteel: 100` |
| Кожа (Leather) | `MedievalPelt` | `MedievalProcessedPelt: 100` |
| Ткань (Cloth) | `MedievalFlax` | `MedievalCloth: 100` |

Стандартный набор компонентов прототипа одежды (см. любой existing файл): `Sprite` (state: icon) +
`Clothing` (тот же sprite) + `Damageable` + `Injurable: MedievalClothing` + `Armor.modifiers.coefficients`
(Blunt/Slash/Piercing/Heat/Structural) + `PhysicalComposition`. Броня — меньшие коэффициенты = больше защиты.

## Паттерн: одна .rsi с несколькими визуальными вариантами

`Uniforms/Jumpsuit/elite_short.rsi` содержит 4 варианта (cloth/halfcloth/manjete/metal) со state-ами
`equipped-INNERCLOTHING-<вариант>` и иконками `icon`, `icon-half`, `icon-manjete`, `icon-metal`.
Решение: 4 отдельных прототипа (`MedievalClothingInnerElite{Cloth,Half,Manjete,Metal}`), каждый
**переопределяет** `Sprite.state` и `Clothing.clothingVisuals.jumpsuit`, потому что базовый
`equipped-INNERCLOTHING` в такой .rsi отсутствует. Варианты наследуются от `...EliteCloth`.

## Импорт пака от спрайтера — типичные проблемы

Спрайтер часто копирует `meta.json` из шаблона outerClothing и оставляет опечатки в именах PNG.
Перед созданием прототипов **нормализовать**:
- `equiped-*` → `equipped-*` (двойная p), `equiped_LEGS`/`equiped_PANTS` → `equipped-LEGS`.
- Переписать `meta.json.states` под фактический слот (а не оставшийся от шаблона `equipped-OUTERCLOTHING`).
- Удалить служебные `База.txt` из .rsi (в .rsi должны быть только `meta.json` + png).
- `-female` спрайты подключаются через sex-специфичные ключи `clothingVisuals` (см. ниже).

## Женская отрисовка (sex-specific спрайты)

Клиент (`Content.Client/Clothing/ClientClothingSystem.cs`, блок `Respiral-Start`) выбирает слой
`clothingVisuals` по цепочке (`{slot}` = **имя слота инвентаря**, не суффикс state):
1. `{slot}-{SpeciesId}-{Sex}` → 2. `{slot}-{SpeciesId}` → 3. `{slot}-{Sex}` (например `outerClothing-Female`)
→ 4. generic `{slot}` → 5. авто-генерация дефолта из RSI (`equipped-{TemporarySlotMap[slot]}`, только species, **без** sex).

Поэтому женский спрайт = ключ `{slot}-Female` со state `equipped-<SUFFIX>-female`, добавляется **только**
если соответствующий `-female` PNG реально есть в .rsi (иначе слой ссылается на несуществующий state).

| Слот (имя инвентаря) | Female-ключ | Female-state |
|---|---|---|
| jumpsuit | `jumpsuit-Female` | `equipped-INNERCLOTHING-female` |
| outerClothing | `outerClothing-Female` | `equipped-OUTERCLOTHING-female` |
| head | `head-Female` | `equipped-HELMET-female` |
| neck | `neck-Female` | `equipped-NECK-female` |
| gloves | `gloves-Female` | `equipped-HAND-female` |
| pants | `pants-Female` | `equipped-LEGS-female` |
| shoes | `shoes-Female` | `equipped-FEET-female` |

**Баг базы (исправлен):** `MedievalBasePants`/`MedievalBaseShoes` в `base.yml` имели ключи
`jumpsuit*` вместо `pants*`/`shoes*` — они никогда не матчились (слот `pants`/`shoes`), отрисовка шла
через дефолт-генератор, женские спрайты игнорировались. Pants-база переключена на `pants`/`pants-Female`
(все 4 штанов имеют female-арт), Shoes-база — на `shoes`/`shoes-Male` (female только у части обуви → её
ключ `shoes-Female` добавляется per-item). Если у предмета есть female-PNG — кладём `{slot}-Female`
в его `clothingVisuals`. Если нет — ничего не делаем, для обоих полов рисуется unisex (это норма).

Сейчас female-арт есть только у 9 предметов нового пака: 4 штанов, 2 внешних доспеха
(Light/Normal), 2 сапог (high/rich), плащ лорда. Остальная одежда female-спрайтов не имеет.

## Стартовое снаряжение

`type: startingGear` с блоком `equipment:` где ключ — слот (`jumpsuit`, `outerClothing`, `head`,
`neck`, `gloves`, `pants`, `shoes`), значение — id прототипа. Привязка профессий — по `База.txt`.
Заполнены все 18 ролей в `Roles/Jobs/{Nobles,Craftsman,Villager,Commoner}` (Lord/Knight/Steward/
Treasurer, кузнецы+страж+торговец+горожане, старейшина/фермер/крестьянин, пилигрим).
