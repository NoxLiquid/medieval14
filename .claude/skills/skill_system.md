# Skill System (прокачка навыков)

## Архитектура

- **SkillSystem** (`Content.Server/_Respiral/Skill/SkillSystem.cs`) — основная система: `AddSkillProgress`, `GetSkillProgress`, `CanLearn`
- **MeleeSkillSystem** (`Content.Server/_Respiral/Skill/MeleeSkillSystem.cs`) — объединённая система для боевых навыков: модификация урона + изучение навыков при ударе
- **SkillPrototype** (`Content.Shared/_Respiral/Skills/Prototypes/SkillPrototype.cs`) — прототип навыка

## Компоненты

### MeleeSkillComponent
Путь: `Content.Server/_Respiral/Skill/Components/MeleeSkillComponent.cs`

| Поле | Тип | Описание |
|---|---|---|
| `Skills` | `List<ProtoId<SkillPrototype>>` | Требуемые навыки (обязательно) |
| `Points` | `List<Dictionary<ProtoId, float>>` | Очки за удар по навыкам (обязательно) |
| `LearnModifiers` | `List<SkillLearnModifier>` | Множители очков по белому списку цели; если цель не подходит ни под один — очки не начисляются |
| `DamageModifiers` | `Dictionary<ProtoId, float>` | Множители урона от прогресса навыка |

### SkillLearnModifier (`[DataDefinition]`)
Путь: определён в `MeleeSkillComponent.cs`

| Поле | Тип | Описание |
|---|---|---|
| `Whitelist` | `EntityWhitelist` | Белый список цели (обязательно) |
| `Modifier` | `float` | Множитель очков (обязательно) |

Модификаторы стекаются мультипликативно.

## Логика MeleeSkillSystem.OnMeleeSkillHit

1. **Урон**: `BaseDamage` умножается на `GetSkillProgress` для каждого навыка
2. **Изучение**: для каждой попавшей сущности:
   - Пропуск если бьёшь сам себя
   - Вычисление множителя из `LearnModifiers` (суммирование совпавших); если 0 — пропуск
   - Начисление `value * modifier` очков за каждый навык

## Пример YAML

```yaml
- type: MeleeSkill
  skills:
  - MedievalOneSword
  points:
  - MedievalOneSword: 0.01
  learnModifiers:
  - whitelist:
      components:
      - MobState
    modifier: 2.0
```

## Ремесленные навыки

| Skill ID | Название | Станок | Книга |
|---|---|---|---|
| MedievalWoodProcessing | обработка дерева | MedievalSawmill, MedievalCarpenterTable | MedievalSkillBookWoodProcessing |
| MedievalStoneProcessing | обработка камня | MedievalQuarry | MedievalSkillBookStoneProcessing |
| MedievalBasicBlacksmithing | кузнечное дело | MedievalAnvil | MedievalSkillBookBasicBlacksmithing |
| MedievalTailor | портное дело | MedievalSewing, MedievalLoom | MedievalSkillBookTailor |
| MedievalAlchem | алхимия | MedievalAlchemTable | MedievalSkillBookAlchem |
| MedievalJewelry | ювелирное дело | MedievalJeweler | MedievalSkillBookJewelry |

## Паттерн создания нового станка

1. Skill → `skill.yml` (type: skill, pointsNeeded: 100)
2. Tag → `tags.yml` (Medieval[Machine]MaterialStorage)
3. Category → `Recipe/category.yml` (type: latheCategory) + locale в `lathe/category.ftl`
4. Recipe pack → `Recipe/packs.yml` (type: latheRecipePack)
5. Machine detail → `Parts/production.yml` (parent: MedievalBaseMachineDetail, type: MachineBoard)
6. Machine entity → `Structures/Machine/lathe.yml` (parent: MedievalBaseLathe, NeededSkillForInteract)
7. Книга → `Skill/book.yml` (parent: MedievalSkillBookBase, LearnSkillWhenUsing)
8. Тег на входные материалы → добавить Medieval[Machine]MaterialStorage к нужным сущностям
9. Граф строительства — общий MedievalMachine (не нужен кастомный)

## История

- `LearnSkillWhenMeleeAttackSystem` и `LearnSkillWhenMeleeAttackComponent` — удалены, функциональность перенесена в `MeleeSkillSystem` и `MeleeSkillComponent`
