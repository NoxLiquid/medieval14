# Material-derived combat stats (durability / armor / damage)

Damage, armor and durability of crafted items are being moved from hand-tuned per-entity values to
**derivation from `PhysicalComposition` × per-material stats**. Same pattern as price
(`MedievalPhysicalPricedSystem` → `PricingSystem.GetPrice` reads composition × `material.price`).

## Per-material stats (source of truth)
Added to `MaterialPrototype` via a Respiral **partial** (`Content.Shared/_Respiral/Materials/MaterialPrototype.Respiral.cs`
— `MaterialPrototype` is `sealed partial`, so NO upstream edit). Values live in the `- type: material` entries
in `Entities/Objects/ProcessedMaterials/materials.yml`.

| Material | damageFactor | protection {Blunt/Slash, Pierce, Heat} @100u | durability /unit |
|---|---|---|---|
| Cloth | – | {0.10, 0.10, 0.04} | 1.0 |
| Pelt/leather | – | {0.14, 0.10, 0.08} | 1.5 |
| Wood | 0.25 | {0.08, 0.06, 0.02} | 1.5 |
| Stone | 0.60 | {0.20, 0.12, 0.10} | 2.0 |
| Iron | 1.00 | {0.22, 0.12, 0.25} | 2.5 |
| Silver | 1.00 | {0.10, 0.05, 0.25} | 2.0 |
| Gold | 0.90 | {0.15, 0.10, 0.30} | 1.8 |
| Steel | 1.25 | {0.27, 0.17, 0.40} | 3.5 |
| Mithril | 1.50 | {0.32, 0.25, 0.40} | 5.0 |
| Adamantine | 1.75 | {0.42, 0.35, 0.45} | 8.0 |
| Glass | 0.80 | {0.05, 0.03, 0.02} | 0.8 |
| Paper | – | – | 0.2 |

## Formulas
Composition `{matᵢ: amountᵢ}`, `T = Σ amountᵢ`:
- **Durability** (Destructible destroy threshold): `Σᵢ amountᵢ × matᵢ.durability`.
- **Armor** (per type t): `protₜ = Σᵢ (amountᵢ/100 × matᵢ.protection[t])`; `coefₜ = clamp(1 − protₜ, 0.4, 1)`.
  **No body-region factor** (removed per user 2026-08): protection depends only on material amount × stat — a
  weaker helmet just uses less material (which is also cheaper). Consequence: a head/hands/feet piece at the
  same material amount as body armor now protects equally; tune via composition `amount`, not a region tier.
- **Weapon damage**: `MeleeWeapon.Damage = BaseDamage × max(matᵢ.damageFactor over composition)`. `BaseDamage`
  is the iron-baseline on `MedievalWeaponDamageComponent` (per weapon); the **primary (highest-factor) material**
  — the blade, not the wood handle — sets the multiplier. Amount doesn't drive damage (archetype fixes it);
  it drives durability/armor/cost.

## Design invariants to preserve
Iron & cloth are the cost-efficient workhorses; higher tiers = absolute power at a price premium (diminishing
stat/price); **silver & gold are deliberately weak for combat** (status metals). Verified via stat/price.

## Derivation system
`Content.Server/_Respiral/Materials/MedievalMaterialStatsSystem.cs` (server, `partial` — one file per phase),
subscribes `PhysicalCompositionComponent, MapInitEvent`, reads composition × `MaterialPrototype` stats.
Marker-less: applies to any composed entity that also has the relevant component (Destructible / Armor /
MeleeWeapon). `DamageTrigger.Damage` is `FixedPoint2` and read live by `DamageTrigger.Reached`, so setting it at
MapInit takes effect immediately (no caching).

## Phase status
- **Phase 1 — Durability: DONE (2026-08).** Sets the destruction `DamageTrigger.Damage` (the threshold whose
  `DoActsBehavior.HasAct(ThresholdActs.Destruction)`) = Σ(amount × durability). Calibrated so iron sword stays
  500. New spread: wood sword 300, iron 500, steel 525, mithril 1000, adamantine 1600; cloth garment 100,
  leather 150, iron chainmail 250, steel plate ~425.
- **Phase 2 — Armor: DONE (2026-08).** Own `MedievalArmorComponent {Region, Modifiers}` +
  `MedievalArmorSystem` (both Shared, `Content.Shared/_Respiral/Armor/`) **replace upstream `ArmorComponent`**
  on medieval clothing — **zero upstream edits** (chosen over touching `ArmorComponent`, which is
  `[Access(SharedArmorSystem)]` + `required`). The system derives `Modifiers` at MapInit from
  `1 − Σ(amount/100 × protection)` (clamp floor 0.4, **no region factor**), applies via
  `InventoryRelayedEvent<DamageModifyEvent>` + `CoefficientQueryEvent`, and reuses the upstream `armor-*` examine
  loc. The 28 `MedievalArmor{Mat}{Region}` abstracts in `base_material.yml` collapsed to a **single marker
  `MedievalArmorMaterial`** (`- type: MedievalArmor`; material AND region-free — comes from composition); ~96
  clothing parents migrated by regex. Body-region coefficients reproduce the old values (iron chainmail 0.78,
  cloth 0.90, steel plate 0.73); non-body pieces (helmets/gloves/boots) are now stronger at equal material
  amount (region tier gone) — reduce their composition `amount` if weaker extremities are wanted. **Fixed a latent
  bug:** the 4 material armors (quilted gambeson / leather jacket / plate / plate+leather) previously had NO worn
  armor (they only had `Damageable.damageModifierSet` = item self-resistance) — they now derive Body armor
  (gambeson 20% blunt, leather 21%, plate+leather 41%).
- **Phase 3 — Damage: DONE (2026-08).** `MedievalDamageFactor` on materials (Wood 0.25 · Iron/Silver 1.0 ·
  Gold 0.9 · Steel 1.25 · Mithril 1.5 · Adamantine 1.75 · Stone 0.6 · Glass 0.8). New `MedievalWeaponDamageComponent
  {BaseDamage}` (iron-baseline, per weapon) + derivation in `MedievalMaterialStatsSystem.ApplyWeaponDamage`
  (server, MapInit): `MeleeWeapon.Damage = BaseDamage × max(damageFactor over composition)`. **`max` (not weighted
  average)** so the wood handle (0.25) doesn't dilute a metal blade. Migration (84 weapons, `Weapon/*.yml`):
  set each weapon's `BaseDamage = orig ÷ maxFactor` (preserving the damage *type*, so wood=Blunt,
  silver=+Holy, fire/spectral blades keep Heat/Cold). **`MeleeWeapon.Damage` is a REQUIRED DataField** — it
  CANNOT be removed (prototype load throws `RequiredFieldNotMappedException`), so the inline
  `MeleeWeapon.damage` stays (= the original final value) as the load-time value; the system OVERWRITES it at
  MapInit with `BaseDamage × maxFactor` (identity now, composition-driven if materials later change). Self-check:
  `base × factor` reproduces all 84 originals exactly (identity migration, no balance change). **Pre-existing
  quirks preserved, not fixed:** some archetypes were internally inconsistent (e.g. WoodMace 10 > IronMace 6.67 →
  wood base 40; knife/eveningstar/polearm steel≠iron×1.25) — the migration keeps current values; rebalance later
  by editing `BaseDamage`/factors, not per-material numbers.

## Verify
`dotnet build` clean. In-game: spawn iron vs adamantine sword, damage them equally → adamantine survives far
longer; a steel plate outlasts a cloth tunic.
