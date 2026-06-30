# Respiral — Global Skills Overview

This file is the entry point for the `.claude/skills/` knowledge base.

## Subsystem Files

| File | Domain |
|---|---|
| `construction.md` | Construction graphs, recipes, structure prototypes |
| `combat.md` | Damage system, modifiers, Destructible balance |
| `interface.md` | UI/HUD: custom alert icons (health, crit, death, bleed, stamina) |
| `mana.md` | Mana system: pool, spending, regeneration via TimeWindow, alert indicator |
| `spell.md` | Spells (Action + form event) and elements (first-class), data-driven, ball as example |
| `containers.md` | EntityStorage, barrels (Barrel), SolutionContainer on structures, visualizers |
| `bureaucracy.md` | Paper, scrolls, folders, passport, stamps (medieval) |
| `identity.md` | Entity recognition: heraldry, passport, Model B (binding by description). DESIGN, partially implemented |
| `economy.md` | Coins, wallet, gemstones as materials |
| `inventory.md` | Inventory slots, clothing visualization, sex-specific sprites |
| `clothing.md` | Clothing prototypes (Medieval*), RSI state conventions, material→damage/composition, pack imports, profession starting gear |
| `flora.md` | Trees and bushes, `MedievalBaseTree`, RandomSprite convention, flora sprite-pack imports |
| `skill_system.md` | Skill system, NeededSkillForMelee, learning modifiers |
| `mobs.md` | NPCs, monsters, factions, base mob prototypes |
| `trade_console.md` | Trade system: ordering via CargoOrderConsole, delivery via CargoPallet (Buy/Sell), selling via CargoPalletConsole |
| `communications.md` | Connecting magic ball (medieval communications console) |
| `disposal.md` | Power-free disposal, link between disposal pipes and tiles |
| `biomes.md` | Biomes, procedural map generation, layers, markers, Gateway |
| `zlevels.md` | Z-levels (vertical maps), CEZLevelsSystem, ZNetwork |
| `localization.md` | Localization (FTL), syncing ru-RU with en-US, parity of variables and message IDs |
| `testing.md` | Integration tests, CI strategy (Respiral blocking / upstream non-blocking), `--filter`, ID prefixes |

## Key Architectural Rules

- All Respiral content lives under `_Respiral/` subdirectories (prototypes, textures, C# code).
- YAML prototype IDs use the `Medieval` prefix (e.g., `MedievalRoofWooden`).
- C# systems/components in `Content.Server/_Respiral/` and `Content.Shared/_Respiral/`.
- ZLevels subsystem uses the `CE` prefix; ported MIT code from CrystallEdge.

## Prototype Inheritance Rules (SS14)

- **Dictionaries** (dict/map) are **merged** on inheritance — a child prototype only adds new keys, parent keys are preserved automatically. Do NOT duplicate upstream keys.
- **Lists** (sequence/list) are **replaced** on inheritance — if you need to keep parent elements, list them explicitly.
- This is critical for `StationCargoOrderDatabase.orders` (dict) vs `.markets` (list) and similar components.
