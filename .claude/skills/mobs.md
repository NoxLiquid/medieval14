# Mobs — NPC и монстры

## Архитектура мобов

Мобы наследуют от цепочки базовых прототипов SS14:
`BaseMob` → `BaseSimpleMob` → `SimpleSpaceMobBase` / `SimpleMobBase`

Respiral добавляет свои базы:
- `MedievalFloraMobBase` — животные (Flora), используют `MedievalBiological` damageContainer
- `MedievalMonsterMobBase` — монстры, враждебные ко всем фракциям

## Фракции (NPC Factions)

Файл: `Resources/Prototypes/_Respiral/NPC/factions.yml`

| Фракция | Поведение |
|---|---|
| `TownPeople` | Жители города |
| `MedievalHerbivoreFlora` | Травоядные |
| `MedievalWolf` | Волки |
| `MedievalCat` | Коты |
| `MedievalBear` | Медведи |
| `MedievalFox` | Лисы |
| `MedievalHog` | Кабаны |
| `MedievalMonster` | Монстры — враждебны ко всем остальным фракциям |

Все фракции взаимно враждебны с `MedievalMonster`.

## Монстры

Файлы:
- База: `Resources/Prototypes/_Respiral/Entities/Mobs/Monster/base.yml`
- Прототипы: `Resources/Prototypes/_Respiral/Entities/Mobs/Monster/monsters.yml`
- Спрайты: `Resources/Textures/_Respiral/Mobs/Monster/{name}.rsi`

Все монстры используют фракцию `MedievalMonster` и AI `MedievalHostileFloraCompound` (из `_Respiral/NPC/flora.yml`).

**ВАЖНО:** сущности из `_Respiral` никогда не должны использовать HTN-компаунды из оригинальной сборки (например `SimpleHostileCompound`). Только Respiral-компаунды: `MedievalHostileFloraCompound` (враждебные), `MedievalHerbivoreFloraCompound` (травоядные).

| ID | Название | HP | Скорость | Урон |
|---|---|---|---|---|
| `MobMedievalBoneHound` | Костяной пёс | 80 | 6 | Brute 8 |
| `MobMedievalCackle` | Кудахта | 60 | 5 | Brute 6 |
| `MobMedievalFlem` | Флем | 100 | 3 | Brute 10 |
| `MobMedievalFlyagaric` | Мухомор | 50 | 3 | Poison 8 |
| `MobMedievalIceSpectre` | Ледяной призрак | 70 | 5 | Cold 10 |
| `MobMedievalInvisibleWhistler` | Невидимый свистун | 40 | 7 | Brute 12 |
| `MobMedievalLumish` | Люмиш | 30 | 3 | Poison 5 |
| `MobMedievalLurker` | Скрытень | 120 | 5 | Brute 15 |
| `MobMedievalMole` | Крот | 60 | 4 | Brute 8 |
| `MobMedievalMosquito` | Комар | 20 | 7 | Piercing 4 |
| `MobMedievalSpineguard` | Шипохвост | 150 | 3.5 | Brute 18 |

## DamageStateVisuals

Стандартная конфигурация из базы: state `alive` для Alive, state `dead` для Dead.
Исключения (нет state `dead` в RSI): `ice_spectre`, `mosquito` — используют `alive` для обоих состояний.
`lumish` — при смерти показывает state `lumishroom`.

## Ключевые компоненты моба

- `MobState` + `MobThresholds` — состояния Alive/Dead с порогом HP
- `DamageStateVisuals` — переключение спрайтов по состоянию
- `NpcFactionMember` — принадлежность к фракции
- `HTN` — AI дерево поведения
- `MeleeWeapon` — атака ближнего боя
- `Fixtures` — физика (коллайдер, масса)
- `MovementSpeedModifier` — скорость передвижения

## Спавнеры профессий (RandomHumanoidSpawner)

Файл: `Resources/Prototypes/_Respiral/Roles/spawners.yml`. Образец — `Entities/Mobs/Player/humanoid.yml`.

Спавнер профессии = пара прототипов:
1. `type: entity` (наследует `BaseMedievalHumanoidSpawner` → спрайт-маркер `Markers/jobs.rsi state: green`)
   с компонентом `RandomHumanoidSpawner` (`settings: <id>`). При MapInit заменяет себя гуманоидом.
2. `type: randomHumanoidSettings` (`id` совпадает с id профессии, как в апстриме ERTLeader) с
   компонентами `RandomHumanoidAppearance` (RandomizeName=true по умолчанию) и
   `Loadout` (`prototypes: [ <Medieval*Gear> ]`).

`Loadout.prototypes` — это `List<ProtoId<StartingGearPrototype>>`, ссылается на `startingGear` из
`_Respiral/Roles/Jobs/*` (которые задают `equipment:` по слотам). Так спавнер одевает гуманоида в
снаряжение профессии. Покрыты все 17 ролей (Nobles/Craftsman/Villager/Commoner).

Не добавлен `GhostRole` (нет ghost-role локали для medieval-профессий) — спавнят инертного NPC, не
ghost-роль. Если нужны ghost-роли — добавить `GhostRole` в settings + строки `ghost-role-information-*`.
