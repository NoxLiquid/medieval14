# Clothing & Starting Gear

Domain: clothing (Respiral medieval clothing), its prototypes, RSI sprites, and profession starting gear.

## Where things live

- **Sprites:** `Resources/Textures/_Respiral/Clothing/<Slot>/*.rsi`
  - `OuterClothing/armor/` — outer armor (outerClothing)
  - `Legs/` — pants/greaves (pants/legs slot)
  - `Shoes/` — footwear (feet)
  - `Head/Hats/`, `Head/Helmets/` — headgear (head)
  - `Neck/` — cloaks/necklaces (neck)
  - `Hands/` — gloves (gloves)
  - `Uniforms/Jumpsuit/` — body clothing/shirts (innerclothing/jumpsuit)
- **Prototypes:** `Resources/Prototypes/_Respiral/Entities/Clothing/`
  - `base.yml` — abstract bases `MedievalBase*` (Outer, Inner, Head, Neck, Gloves, Pants, Shoes, …)
  - `outer/armor.yml`, `legs.yml`, `shoes.yml`, `head.yml`, `neck.yml`, `gloves.yml`, `inner.yml`
- **Starting gear:** `Resources/Prototypes/_Respiral/Roles/Jobs/*.yml` (`type: startingGear`)

## Base prototypes and their sprite states

Each `MedievalBase*` sets `Clothing.clothingVisuals` with a fixed state name.
**The PNG names in the .rsi must match these states**, otherwise the sprite won't render / the engine crashes.

| Slot | Base prototype | equipment key | Expected states |
|---|---|---|---|
| innerclothing | `MedievalBaseInnerClothing` | `jumpsuit` | `equipped-INNERCLOTHING` |
| outerClothing | `MedievalBaseOuterClothing` | `outerClothing` | `equipped-OUTERCLOTHING` |
| head | `MedievalBaseHead` | `head` | `equipped-HELMET` |
| neck | `MedievalBaseNeck` | `neck` | `equipped-NECK` |
| gloves | `MedievalBaseGloves` | `gloves` | `equipped-HAND` |
| legs | `MedievalBasePants` | `pants` | `equipped-LEGS`, `equipped-LEGS-female` |
| feet | `MedievalBaseShoes` | `shoes` | `equipped-FEET`, `equipped-FEET-female` |

All .rsi also contain `icon`, `inhand-left`, `inhand-right`.
The equipment slot keys come from `Resources/Prototypes/InventoryTemplates/human_inventory_template.yml`
(the pants slot is called `pants`, the body slot `jumpsuit`).

## Material → damage modifier / composition

| Material (from `База.txt`) | `damageModifierSet` | `PhysicalComposition` |
|---|---|---|
| Iron | `MedievalIron` | `MedievalProcessedIron: 100` |
| Steel | `MedievalSteel` | `MedievalSteel: 100` |
| Leather | `MedievalPelt` | `MedievalProcessedPelt: 100` |
| Cloth | `MedievalFlax` | `MedievalCloth: 100` |

The standard component set of a clothing prototype (see any existing file): `Sprite` (state: icon) +
`Clothing` (same sprite) + `Damageable` + `Injurable: MedievalClothing` + `Armor.modifiers.coefficients`
(Blunt/Slash/Piercing/Heat/Structural) + `PhysicalComposition`. Armor — lower coefficients = more protection.

## Pattern: one .rsi with several visual variants

`Uniforms/Jumpsuit/elite_short.rsi` contains 4 variants (cloth/halfcloth/manjete/metal) with states
`equipped-INNERCLOTHING-<variant>` and icons `icon`, `icon-half`, `icon-manjete`, `icon-metal`.
Solution: 4 separate prototypes (`MedievalClothingInnerElite{Cloth,Half,Manjete,Metal}`), each
**overriding** `Sprite.state` and `Clothing.clothingVisuals.jumpsuit`, because the base
`equipped-INNERCLOTHING` is absent in such an .rsi. The variants inherit from `...EliteCloth`.

## Importing a pack from a spriter — typical problems

The spriter often copies `meta.json` from an outerClothing template and leaves typos in PNG names.
Before creating prototypes, **normalize**:
- `equiped-*` → `equipped-*` (double p), `equiped_LEGS`/`equiped_PANTS` → `equipped-LEGS`.
- Rewrite `meta.json.states` for the actual slot (not the leftover template `equipped-OUTERCLOTHING`).
- Remove service `База.txt` files from the .rsi (an .rsi should contain only `meta.json` + png).
- `-female` sprites are wired in via sex-specific `clothingVisuals` keys (see below).

## Female rendering (sex-specific sprites)

The client (`Content.Client/Clothing/ClientClothingSystem.cs`, block `Respiral-Start`) picks the
`clothingVisuals` layer via a chain (`{slot}` = **inventory slot name**, not the state suffix):
1. `{slot}-{SpeciesId}-{Sex}` → 2. `{slot}-{SpeciesId}` → 3. `{slot}-{Sex}` (e.g. `outerClothing-Female`)
→ 4. generic `{slot}` → 5. auto-generated default from RSI (`equipped-{TemporarySlotMap[slot]}`, species only, **no** sex).

So a female sprite = key `{slot}-Female` with state `equipped-<SUFFIX>-female`, added **only**
if the corresponding `-female` PNG actually exists in the .rsi (otherwise the layer references a nonexistent state).

| Slot (inventory name) | Female key | Female state |
|---|---|---|
| jumpsuit | `jumpsuit-Female` | `equipped-INNERCLOTHING-female` |
| outerClothing | `outerClothing-Female` | `equipped-OUTERCLOTHING-female` |
| head | `head-Female` | `equipped-HELMET-female` |
| neck | `neck-Female` | `equipped-NECK-female` |
| gloves | `gloves-Female` | `equipped-HAND-female` |
| pants | `pants-Female` | `equipped-LEGS-female` |
| shoes | `shoes-Female` | `equipped-FEET-female` |

**Base bug (fixed):** `MedievalBasePants`/`MedievalBaseShoes` in `base.yml` had keys
`jumpsuit*` instead of `pants*`/`shoes*` — they never matched (slot `pants`/`shoes`), rendering went
through the default generator, and female sprites were ignored. The Pants base was switched to `pants`/`pants-Female`
(all 4 pants have female art), the Shoes base — to `shoes`/`shoes-Male` (female only on some footwear → its
`shoes-Female` key is added per-item). If an item has a female PNG — add `{slot}-Female`
to its `clothingVisuals`. If not — do nothing, the unisex sprite is drawn for both sexes (that's normal).

Currently female art exists only on 9 items of the new pack: 4 pants, 2 outer armors
(Light/Normal), 2 boots (high/rich), the lord's cloak. The rest of the clothing has no female sprites.

## Starting gear

`type: startingGear` with an `equipment:` block where the key is the slot (`jumpsuit`, `outerClothing`, `head`,
`neck`, `gloves`, `pants`, `shoes`) and the value is the prototype id. Profession assignments — per `База.txt`.
All 18 roles in `Roles/Jobs/{Nobles,Craftsman,Villager,Commoner}` are filled (Lord/Knight/Steward/
Treasurer, blacksmiths + guard + merchant + townsfolk, elder/farmer/peasant, pilgrim).

## Clothing-set cleanup (2026-06)

The `Clothing/Sets/` folder was a temporary staging area (per-profession copies of sprites). The user
manually left only the "correct" sprites in it, after which:
- the surviving RSIs were redistributed back into the `Clothing/` slot folders at canonical paths matching
  the prototypes' `sprite:` (slot determined by the `equipped-*.png` inside the RSI);
- **53 clothing prototypes** whose sprites no longer exist were removed from `Entities/Clothing/*.yml`
  (the emptied `outer/common.yml`, `outer/mantles.yml`, `outer/robes.yml` were deleted entirely);
- **46 recipes** (`latheRecipe`) in `Recipe/recipe.yml` for this clothing were removed;
- slots with removed prototypes were taken out of `Roles/Jobs/Nobles/nobles.yml` (Lord — `outerClothing`;
  Knight — `outerClothing` and `pants`);
- the `Clothing/Sets/` folder was deleted.

Invariant after cleanup: **every clothing prototype → an existing RSI**, and **every RSI under
`Clothing/` → exactly one prototype** (0 broken references, 0 orphan sprites — verified by script).

Prototypes were added for the surviving new sprites:
`MedievalClothingShoesBoots` (`Shoes/boots.rsi`), `MedievalClothingShoesHighBoots`
(`Shoes/high_boots.rsi`), `MedievalClothingShoesRichBoots` (`Shoes/rich_boots.rsi`),
`MedievalClothingLegsSimple` (`Legs/simple_pants.rsi`). They have no recipes or role bindings yet.

Fixed: in `iron_chainmail.rsi` the equipped state was renamed
`equipped-INNERCLOTHING` → `equipped-OUTERCLOTHING` (in the file and meta.json; the typo
`quipped-…` was also removed) so the chainmail `MedievalClothingOuterArmorIronChainmail` renders correctly in the
`outerClothing` slot.

Second removal pass (old base clothing): sprites + prototypes + recipes removed for
`MedievalClothingShoesLeather/High/Rich/Straw` (leather/high_boot/rich_boot/straw),
`MedievalClothingInnerSimpleShirt/TrimmedShirt/PeasantRags` (shirt/shirt2/peasant-rags). The
corresponding `shoes:`/`jumpsuit:` lines were cleaned out of all roles.
- The **shoes** slot is now covered only by the new `MedievalClothingShoesBoots/HighBoots/RichBoots`.
- The **jumpsuit** slot: `MedievalClothingInnerLongTunic` + Elite variants remain
  (`EliteCloth/EliteHalf/EliteManjete/EliteMetal`).
- ALL professions are currently **without footwear** (and many without jumpsuit) — starting gear awaits reassignment
  to surviving/new prototypes.

## New armored-clothing pack (2026-06)

Prototypes added for Twilight-Fortress sprites (CC-BY-SA-3.0):
- `MedievalClothingShoesArmoredBoots` (`Shoes/armored_boots.rsi`) — "armored boots",
  iron (`MedievalIron` / `MedievalProcessedIron: 100`), coef. ~0.92, has `shoes-Female`.
- `MedievalClothingShoesHeavyArmoredBoots` (`Shoes/heavy_armored_boots.rsi`) — "heavy
  armored boots", steel (`MedievalSteel`), coef. ~0.86, has `shoes-Female`.
- `MedievalClothingHandsHeavyArmored` (`Hands/heavy_armored.rsi`) — "heavy armored
  gloves", steel, coef. ~0.88, no female art (only `equipped-HAND`).

These .rsi **do not contain** `inhand-left`/`inhand-right` (meta.json has only icon + equipped-*),
unlike the previously imported RSIs. The base `Item.inhandVisuals` references missing
states — the in-hand sprite won't render (the engine doesn't crash). No role bindings yet.

Crafting recipes added (all three metallic, parent `MedievalBaseMetalClothingRecipe`,
`completetime` 10, category `MedievalMetalClothingCategory`):
`MedievalClothingShoesArmoredBootsRecipe` (`MedievalProcessedIron: 100`),
`MedievalClothingShoesHeavyArmoredBootsRecipe` (`MedievalSteel: 100`),
`MedievalClothingHandsHeavyArmoredRecipe` (`MedievalSteel: 100`). The recipe materials match
the item's `PhysicalComposition`. Registered in the anvil pack `Recipe/packs.yml`
(next to `MedievalClothingShoesChainRecipe`/`MedievalClothingHandsChainGlovesRecipe`) — otherwise
the recipe won't appear in the machine (packs are static, not category-based).
