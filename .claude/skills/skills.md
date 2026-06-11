# Respiral — Global Skills Overview

This file is the entry point for the `.claude/skills/` knowledge base.

## Subsystem Files

| File | Domain |
|---|---|
| `construction.md` | Construction graphs, recipes, structure prototypes |
| `combat.md` | Damage system, modifiers, Destructible balance |
| `containers.md` | EntityStorage, бочки (Barrel), SolutionContainer на структурах, визуализаторы |
| `bureaucracy.md` | Бумага, свитки, папки, паспорт, штампы (средневековые) |
| `economy.md` | Монеты, кошелёк, драгоценные камни как материалы |
| `inventory.md` | Слоты инвентаря, визуализация одежды, sex-specific спрайты |
| `skill_system.md` | Система навыков, MeleeSkill, модификаторы изучения |
| `mobs.md` | NPC, монстры, фракции, базовые прототипы мобов |
| `trade_console.md` | Торговая система: заказ через CargoOrderConsole, доставка через CargoPallet (Buy/Sell), продажа через CargoPalletConsole |
| `communications.md` | Связующий магический шар (средневековая консоль коммуникаций) |
| `biomes.md` | Биомы, процедурная генерация карт, слои, маркеры, Gateway |
| `zlevels.md` | Z-уровни (вертикальные карты), CEZLevelsSystem, ZNetwork |
| `localization.md` | Локализация (FTL), синхронизация ru-RU с en-US, паритет переменных и message ID |
| `testing.md` | Интеграционные тесты, CI-стратегия (Respiral блокирующий / апстрим неблокирующий), `--filter`, префиксы ID |

## Key Architectural Rules

- All Respiral content lives under `_Respiral/` subdirectories (prototypes, textures, C# code).
- YAML prototype IDs use the `Medieval` prefix (e.g., `MedievalRoofWooden`).
- C# systems/components in `Content.Server/_Respiral/` and `Content.Shared/_Respiral/`.
- ZLevels subsystem uses the `CE` prefix; ported MIT code from CrystallEdge.

## Prototype Inheritance Rules (SS14)

- **Словари** (dict/map) при наследовании **мержатся** — дочерний прототип добавляет только новые ключи, родительские сохраняются автоматически. НЕ дублировать апстримные ключи.
- **Списки** (sequence/list) при наследовании **заменяются** — если нужно сохранить родительские элементы, перечислять их явно.
- Это критично для `StationCargoOrderDatabase.orders` (dict) vs `.markets` (list) и аналогичных компонентов.
