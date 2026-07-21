# Combat & Damage System

## Damage Containers

| ID | Supported |
|---|---|
| `MedievalInorganic` | Brute, Burn, Structural |
| `MedievalBiological` | Brute, Burn, Toxin, Airloss, Genetic |
| `MedievalClothing` | Brute, Burn |

## Damage Modifier Sets (structures)

| ID | Blunt | Slash | Pierce | Heat | Structural |
|---|---|---|---|---|---|
| `MedievalWood` | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 |
| `MedievalStone` | 0.7 | 0.7 | 0.7 | 0.3 | 1.0 |
| `MedievalIron` | 0.55 | 0.55 | 0.75 | 0.5 | 1.0 |
| `MedievalSteel` | 0.45 | 0.45 | 0.65 | 0.2 | 1.0 |
| `MedievalMithril` | 0.35 | 0.35 | 0.50 | 0.2 | 1.0 |
| `MedievalAdamantine` | 0.15 | 0.15 | 0.30 | 0.1 | 1.0 |
| `MedievalGold` | 0.7 | 0.7 | 0.8 | 0.4 | 1.0 |
| `MedievalSilver` | 0.8 | 0.8 | 0.9 | 0.5 | 1.0 |

Note: `DamageTrigger` checks accumulated damage **after** modifiers.

## Magic Swords (non-craftable)

- `Resources/Prototypes/_Respiral/Entities/Items/Weapon/magic.yml`
- `MedievalFireBlade` (fire blade) — Slash 24 + Heat 12; `MedievalSpectralBlade` (spectral blade) — Slash 24 + Cold 12
- Both inherit `MedievalBaseSword`, made of `MedievalProcessedAdamantine` (200) + `MedievalRawManaCrystal` (100), `MedievalAdamantine` modifier set, Destructible 1500
- Their RSIs (`spectral_blade.rsi`, `fire_blade.rsi`) have **no blade/handle layers** — only an animated `icon` state + `inhand-left/right`. The `Sprite` component must be overridden to a single `state: icon` (not the base blade/handle layers), and `inhandVisuals` use plain `inhand-left/right` with no color tint.
- **IMPORTANT (2026-06):** setting `sprite:` + `state: icon` is not enough — `MedievalBaseSword` carries `layers: [blade, handle]`, and without overriding `layers:` they get inherited and look for nonexistent `blade`/`handle` states in the icon RSI → YAMLLinter crashed on CLIENT (`State 'blade'/'handle' not found`). Both blades in `magic.yml` got `layers: [ state: icon ]`. Same trap as with `MedievalBaseSpear` (see below).
- Intentionally **not craftable**: no recipe in `recipe.yml`, no construction graph. Spawn/loot only.
- Heat/Cold are in the `Burn` group → applies to mobs (`MedievalBiological` supports Burn).

## Wieldable In-Hand Visuals

- `GenericVisualizer` for `WieldableVisuals.Wielded` was removed from base weapons — it required a non-existent `wielded` RSI state and a missing `Appearance` component
- Wielded in-hand display is handled via `HeldPrefix` ("wielded") set by `SharedWieldableSystem`
- `ItemSystem.OnGetVisuals` (Respiral modification) now applies `HeldPrefix` to explicit `inhandVisuals` layers: if `wielded-{state}` exists in the RSI, it replaces the original state
- This allows weapons with explicit `inhandVisuals` (for per-material color tinting) to also support wielded sprites
- RSIs for two-handed weapons need both `inhand-left/right` and `wielded-inhand-left/right` states

## Primitive crafted weapons (stone + sticks, 2026-06)

File: `Entities/Items/Weapon/crafted.yml`. mars33_2.0 sprites (CC-BY-SA-3.0), path
`_Respiral/Objects/Items/Weapon/{stone_axe,handmade_spear}.rsi`.

| ID | Parent | Damage | Skill | damageModifierSet / HP |
|---|---|---|---|---|
| `MedievalStoneAxe` (stone axe) | `BaseItem` | Blunt 4 / Slash 2 | MedievalOneSword | MedievalStone / 150 |
| `MedievalHandmadeSpear` (handmade spear) | `MedievalBaseSpear` | Piercing 2.5 / Blunt 0.5 (+ wield Piercing 2 / Blunt 1) | MedievalAncientWeapons (from base) | MedievalWood / 150 |

- **Crafted via Construction, NOT lathe** (requirement): recipes in `Construction/items.yml`
  (`MedievalCraftStoneAxe`, `MedievalCraftHandmadeSpear`, `category: construction-category-tools`,
  `objectType: Item`), graphs in `Construction/Graph/Items/weapons.yml`. Raw material: `MedievalRawStone` +
  `MedievalRodWood` (sticks; axe 1+1, spear 1+2). The entities carry `- type: Construction graph/node`
  (mandatory, otherwise ConstructionPrototypeTest fails — see construction.md).
- The axe is one-handed (`BaseItem` + full `MeleeWeapon`); the spear is two-handed wieldable from
  `MedievalBaseSpear`.
- **Back-slot:** both can be slung over the back — `- type: Clothing slots:[back]`, `clothingVisuals.back`
  → state `equipped-BACKPACK` (the spriter drew "On the back"). `quickEquip: false`.

### Pitfalls of importing weapon sprite-packs (mars33_2.0)
- The pack arrived in a Cyrillic path `ФЭНТЕЗИ!!!/Предметы/.../Оружие/` without `meta.json`, with PNG names
  in Cyrillic/with spaces (`Левая`, `Правая`, `В двух,`, `На спине`). Moved to an ASCII namespace,
  old folders deleted.
- **icon was 32×32, but inhand/back/wielded were 64×64.** In one RSI all states must be the same
  size → the whole pack was brought to 64×64, with the icon padded with transparency in the center. The RSI size of a Respiral
  weapon is usually 32×32, but 64×64 is acceptable (precedent: `two_polearm.rsi`).
- The two-handed sprite "In both hands" was kept as state `wielded` (don't lose the art), but it's
  not yet used in the components — there's no auto inhand-swap on wielding in these prototypes.
- When inheriting from a base with a multi-layer `Sprite` (`MedievalBaseSpear` = layers blade/handle)
  always **override `layers`** in the child (`layers: [state: icon]`), otherwise the parent's
  blade/handle are pulled into the new RSI where those states don't exist.

## Friendly Faction (no friendly fire)

`FriendlyFactionComponent` (`Content.Shared/_Respiral/FriendlyFaction/`) is an **opt-in marker**
(no data). `FriendlyFactionSystem` (Shared) blocks melee from a marked entity toward anyone its
faction considers friendly.

- Hook: `AttackAttemptEvent` (raised on the **attacker**, **per target**, cancellable) — same
  mechanism as pacifism (`PacificationSystem`). Predicted on client.
- Friendliness check is **delegated to `NpcFactionSystem.IsEntityFriendly(attacker, target)`** — the
  single source of truth is `NpcFactionMemberComponent.Factions`. Do NOT keep a duplicate faction
  list on the marker. This also honors prototype-declared `Friendly` faction relations
  (`_Respiral/NPC/factions.yml`), not just shared membership.
- Do **not** use `MeleeHitEvent` for this: it is raised on the **weapon** (works only for unarmed,
  where weapon == attacker) and its `BonusDamage` applies to the whole wide-swing at once.
- There is no `BeforeMeleeHit` event. Pre-damage melee hooks: `AttemptMeleeEvent` (cancels the whole
  attack), `MeleeHitEvent` (weapon-side bonus damage/cancel), `GetMeleeDamageEvent` (damage calc).
- Disarms are allowed between friendlies (no damage).
- Opt-in by design: only entities carrying the marker refrain from hitting friendlies. To make it
  global, drop the marker and subscribe `NpcFactionMemberComponent` to `AttackAttemptEvent` instead.
