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
| `blacksmithing.md` | Forge/anvil crafting (planned); current stub prototypes (pliers, bellows, grindstone) |
| `identity.md` | Entity recognition: heraldry, passport, Model B (binding by description). DESIGN, partially implemented |
| `economy.md` | Coins, wallet, gemstones as materials |
| `material_color.md` | `MaterialColor` component: tint sprite/in-hand layers from a material's `color` (single source of truth) |
| `inventory.md` | Inventory slots, clothing visualization, sex-specific sprites |
| `clothing.md` | Clothing prototypes (Medieval*), RSI state conventions, material→damage/composition, pack imports, profession starting gear |
| `flora.md` | Trees and bushes, `MedievalBaseTree`, RandomSprite convention, flora sprite-pack imports |
| `skill_system.md` | Skill system, NeededSkillForMelee, learning modifiers |
| `professions.md` | Roles map: departments/jobs ↔ skills ↔ machines (lathes), starting gear, gaps |
| `mobs.md` | NPCs, monsters, factions, base mob prototypes; mount/saddle/riding system |
| `rope.md` | Rope item: tie two entities together (pull + JointVisuals line), untie verb |
| `hitch.md` | Hitch a cart to a horse: pull to follow + copy horse rotation to face movement; attach/detach verbs |
| `trade_console.md` | Trade system: ordering via CargoOrderConsole, delivery via CargoPallet (Buy/Sell), selling via CargoPalletConsole |
| `communications.md` | Connecting magic ball (medieval communications console) |
| `round_goal.md` | Round goal: Viscount's decree teleported to `MedievalTeleportTable` (reskinned fax) at round start |
| `disposal.md` | Power-free disposal, link between disposal pipes and tiles |
| `tiles.md` | Floor/tile prototypes, wood plank variants (Dead/Birch/Swamp/Dark/Burned), base+child pattern |
| `wallpaper.md` | Wall wallpapers: item→SpawnAfterInteract→structure (carpet-style), drawdepth Walls + SpriteFade, sewn on MedievalSewing lathe |
| `inhand_icon.md` | Client in-hand fallback: items with no inhand sprite show 0.3 icon in hand, over/under body per hand+facing |
| `biomes.md` | Biomes, procedural map generation, layers, markers, Gateway |
| `zlevels.md` | Z-levels (vertical maps), CEZLevelsSystem, ZNetwork |
| `localization.md` | Localization (FTL), syncing ru-RU with en-US, parity of variables and message IDs |
| `testing.md` | Integration tests, CI strategy (Respiral blocking / upstream non-blocking), `--filter`, ID prefixes |
| `engine_submodule.md` | RobustToolbox engine pin, `no-submodule-update` guard, upstream-merge policy, slnx/engine-version coupling |
| `upstream_patches.md` | Map of 56 inline `// Respiral` edits to upstream files; why `_Respiral` can't be a separate `.csproj`; merge-review checklist |
| `respiral_csproj_split.md` | PLANNED future work: extract `_Respiral`/`_CE` into `Content.Respiral.*` assemblies; Phase 0 decoupling (13 refs + 4 partials), blockers, execution plan |

## Key Architectural Rules

- All Respiral content lives under `_Respiral/` subdirectories (prototypes, textures, C# code).
- YAML prototype IDs use the `Medieval` prefix (e.g., `MedievalRoofWooden`).
- C# systems/components in `Content.Server/_Respiral/` and `Content.Shared/_Respiral/`.
- ZLevels subsystem uses the `CE` prefix; ported MIT code from CrystallEdge.

## Prototype Inheritance Rules (SS14)

- **Dictionaries** (dict/map) are **merged** on inheritance — a child prototype only adds new keys, parent keys are preserved automatically. Do NOT duplicate upstream keys.
- **Lists** (sequence/list) are **replaced** on inheritance — if you need to keep parent elements, list them explicitly.
- This is critical for `StationCargoOrderDatabase.orders` (dict) vs `.markets` (list) and similar components.
