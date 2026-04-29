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

## Key Architectural Rules

- All Respiral content lives under `_Respiral/` subdirectories (prototypes, textures, C# code).
- YAML prototype IDs use the `Medieval` prefix (e.g., `MedievalRoofWooden`).
- C# systems/components in `Content.Server/_Respiral/` and `Content.Shared/_Respiral/`.
- ZLevels subsystem uses the `CE` prefix; ported MIT code from CrystallEdge.
