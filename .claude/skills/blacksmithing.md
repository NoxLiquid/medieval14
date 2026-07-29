# Blacksmithing

Forge/anvil crafting with heat, quality, and quenching mechanics (`_Respiral/Blacksmithing/` in
Content.Server/Content.Shared per CLAUDE.md). As of 2026-07-29, only stub entity prototypes exist —
no C# systems under `_Respiral/Blacksmithing/` yet, no gameplay mechanics wired up.

## Stub prototypes (2026-07-29, no functionality yet)

Placeholder entities added for sprites that already existed in `Resources/Textures/_Respiral/`.
Minimal components only (`Sprite` + inherited base) — no `Tool`, `MeleeWeapon`, `Construction`,
`Damageable`/`Destructible`, or `PhysicalComposition`. Add those when the actual mechanic is
implemented (see `MedievalHammer`/`MedievalPickaxe` in the same file for the fully-wired pattern).

| ID | Name | Parent | File | Sprite |
|---|---|---|---|---|
| `MedievalPliers` | клещи | `BaseItem` | `Entities/Items/Tools/tool.yml` | `_Respiral/Objects/Items/Tools/pliers.rsi` (icon, inhand-left/right) |
| `MedievalBellows` | кузнечные меха | `BaseItem` | `Entities/Items/Tools/tool.yml` | `_Respiral/Objects/Items/Tools/bellows.rsi` (icon, inhand-left/right, + unused `inflated` state for future animation) |
| `MedievalGrindstone` | точильный камень | `BaseStructureDynamic` | `Entities/Structures/Machine/grindstone.yml` | `_Respiral/Structures/Machines/grindstone.rsi` (icon only) |

## Inhand sprite convention note

When RSI states are named plainly `inhand-left`/`inhand-right` (no prefix), the `Item` component
needs no `heldPrefix` — it derives inhand sprites straight from the `Sprite` component's rsi+state.
Confirmed against upstream `MedievalShovel`/`shovel.rsi` (same bare `inhand-left`/`inhand-right`
naming, no `heldPrefix` set). Only set `heldPrefix` when states are prefixed, e.g. `MedievalHammer`
→ `heldPrefix: hammer2` for `hammer2-inhand-left` etc.

## Gaps

- No forge/heat/quenching system implemented yet (see CLAUDE.md's description of the planned
  mechanic).
- `MedievalGrindstone` is not yet a `Lathe`/`Machine` — compare `lathe.yml` (`MedievalAnvil`,
  `MedievalSmelter`) in `skill_system.md`/`construction.md`-adjacent machine patterns for what a
  fully-wired forge station looks like once sharpening mechanics exist.
- Sprites have no construction graph / recipe yet — not craftable, only spawnable via admin menu.
