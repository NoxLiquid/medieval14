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
  - `peasant.yml` — commoner set (shirts w/ collar, cloth pants, leather boots), themed file (not slot-split)
  - `refugee.yml` — refugee/settler rags (already-colored sprites, no tint, no craft). **Fragile**: override
    `Destructible` with a low `damage` threshold (~25 vs base 100) so the garment breaks easily. No matching
    role/starting-gear yet (closest role: Refugee) — items only.

## Grayscale + per-layer tint (color variants without extra sprites)
Commissioned grayscale sprites are recolored per prototype via `color:` (multiply). Realistic muted
palette — see `peasant.yml`. Two ways:
- **Single-layer piece** (pants/boots): tint the item icon with component-level `Sprite.color`, then
  repeat the hex on each `Clothing.clothingVisuals` layer and each `Item.inhandVisuals` entry.
- **Two-tone piece** (shirt + collar): shirt body and collar are separate STATES in one RSI
  (`peasant_shirt.rsi`: `equipped-INNERCLOTHING` + `collar-equipped-INNERCLOTHING`, plus `icon`/`collar-icon`,
  `inhand-*`/`collar-inhand-*`). Cannot use component-level color (would tint both the same); instead give
  each of Sprite/Clothing/Item **two layers**, each with its own `color:`. This yields colored shirts with
  a contrasting collar from ONE grayscale RSI.

## Sprite tools (`Tools/_Respiral/`)
- `grayscale_rsi.py <path...>` — PNGs (file or .rsi dir, recursive) → Rec.709-luma grayscale in place,
  alpha preserved. Preps colored art for the tint convention above. Multiply-tint only darkens, so very
  dark source art stays dark.
- `split_color_layers.py [--out DIR] [--hue-tol DEG] [--sat-thresh S] <path...>` — splits a sprite into
  per-colour-group layer PNGs (e.g. mantle → blue body / white trim / gold trim). Chromatic colours are
  single-link chained by hue tolerance; low-sat colours bucketed white/grey/black. Colours pooled across
  all inputs so icon + equipped-* split into matching layers. Output `<stem>_<name>.png`; names are hue
  bins (golden ~40° → `orange`). Layers can then become separately-tintable clothing layers.

## Sprite export gotcha: pants exported as INNERCLOTHING
Artists sometimes export legs garments as `equipped-INNERCLOTHING`. The `pants` slot needs
`equipped-LEGS` (+ `equipped-LEGS-female`). Rename the file to `equipped-LEGS.png` and duplicate it as
`equipped-LEGS-female.png` (base `MedievalBasePants` references the female state). Frame size is 32 (icon
= one 32px frame; equipped/inhand = 2×2 grids of 32px = 4 directions in a 64×64 png).
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
`Clothing` (same sprite) + `Damageable` + `Injurable: MedievalClothing` + **armor via inherited parent**
(see below) + `PhysicalComposition`. Armor — lower coefficients = more protection.

## Armor protection = inherited, NOT inline (2026-08 refactor)

**Do not hand-write `Armor.modifiers.coefficients` on clothing anymore.** The single source of the
protection numbers is `Entities/Clothing/base_material.yml`: 28 `abstract: true` entity prototypes
`MedievalArmor{Material}{Region}` (Material ∈ Cloth/Iron/Steel/Silver/Mithril/Gold/Adamantine,
Region ∈ HeadOrLegs/Body/HeadFeet/Other), each carrying just a `- type: Armor` with its coefficients.
A real clothing piece gets its protection by **multi-parent inheritance**:
```yaml
- type: entity
  parent: [ MedievalBaseOuterClothing, MedievalArmorIronBody ]   # base + armor mixin
  id: MedievalClothingOuterArmorIronChainmail
  # ...no type: Armor block here — inherited from MedievalArmorIronBody
```
To change a material/region's protection, edit the one abstract prototype; every piece inheriting it
updates. To add a new armored piece, pick the matching `MedievalArmor…` parent by material+region.

Why this and not a `damageModifierSet` reference: `ArmorComponent.Modifiers` is an *inline*
`DamageModifierSet` (not a `ProtoId`), and the component is `[Access(SharedArmorSystem)]`, so neither a
YAML prototype-reference nor a Respiral component could set it without an **upstream** edit to
`Content.Shared/Armor/ArmorComponent.cs` (a custom serializer). We deliberately avoided the upstream
edit; abstract-parent inheritance is pure `_Respiral` YAML. See [[skills]] inheritance rules
(components merge across multiple parents; only the abstract mixin defines `Armor`, bases define none →
clean union).

**Deleted:** `Resources/Prototypes/_Respiral/Damage/Modif/clothing.yml` — its 28 `damageModifierSet`
prototypes (`MedievalIronBody`, `MedievalClothHeadFeet`, …) were fully orphaned (nothing referenced
them; `ArmorComponent` can't) and were the duplicated copy of these same numbers. Removed to keep one
source. If a `DamageModifierSet` *prototype* is ever needed again (e.g. for a `Damageable`), re-add just
that one.

**Migration complete (2026-08, part 2):** the 5 pieces above were reparented too, to their
*closest-fit* `MedievalArmor…` mixin (picked by summed absolute coefficient difference across all
4 candidates of the matching material — not an exact match, but nearest). **Zero clothing entities
in `_Respiral` carry an inline `- type: Armor` anymore** — every one either has no armor or inherits
one of the 28 abstracts.

This is what revealed the purpose of the `HeadOrLegs` region tier, unused by the first (exact-match)
pass: it's for **heavy reinforced extremity armor** (gauntlets, greaves, armored boots) — tougher than
the `HeadFeet` tier (light headwear/footwear) but not full `Body` armor. All 4 metal bespoke pieces
landed on `…HeadOrLegs`:
```
MedievalClothingHandsHeavyArmored      -> MedievalArmorSteelHeadOrLegs  (diff 0.04)
MedievalClothingLegsArmored            -> MedievalArmorIronHeadOrLegs   (diff 0.02)
MedievalClothingShoesArmoredBoots      -> MedievalArmorIronHeadOrLegs   (diff 0.03)
MedievalClothingShoesHeavyArmoredBoots -> MedievalArmorSteelHeadOrLegs  (diff 0.10, weakest fit but still nearest)
MedievalClothingHandsSmithGloves       -> MedievalArmorClothOther       (diff 0.04)
```
`MedievalClothingHandsSmithGloves` (cloth) went to `ClothOther` (accessory tier) instead — its Blunt/
Slash/Piercing matched `ClothOther` exactly, only Heat differed (0.95 vs 0.99); leather work-gloves fit
the "light accessory" semantics better than "reinforced limb armor". If exact values matter later, the
piece's own protection can drift a little from what it used to be — this is a deliberate approximation
trade for having one source of truth; revert to an inline `- type: Armor` on that one entity if a
closest-fit isn't good enough for a specific item.

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
## Female rendering — REMOVED (2026-08)

The sex-specific `clothingVisuals` prefix logic was **removed** as unnecessary. What was deleted:
- The `Respiral-Start/End` block in `Content.Client/Clothing/ClientClothingSystem.cs` `OnGetVisuals`
  (the `{slot}-{Sex}` / `{slot}-{SpeciesId}-{Sex}` lookup chain). It's back to plain upstream:
  species lookup `{slot}-{SpeciesId}` → generic `{slot}` → auto-generated default.
- All `{slot}-Male` / `{slot}-Female` keys (with `equipped-*-female` states) from `base.yml`
  (`MedievalBasePants`, `MedievalBaseShoes`) and every per-item override (`guard.yml`, `neck.yml`,
  `peasant.yml`, `shoes.yml`). Every clothing now has only the single unisex `{slot}` key.

Do **not** reintroduce `{slot}-Male`/`{slot}-Female` clothingVisuals keys. Unisex `equipped-*` only.
(The upstream `femaleDisplacements` on the species `Inventory` — e.g. `Body/Species/human.yml`,
`state: jumpsuit-female` — is a **different**, still-active feature for female body-shape displacement maps; leave it.)

## Starting gear

`type: startingGear` with an `equipment:` block where the key is the slot (`jumpsuit`, `outerClothing`, `head`,
`neck`, `gloves`, `pants`, `shoes`) and the value is the prototype id. Profession assignments — per `База.txt`.
All 18 roles in `Roles/Jobs/{Nobles,Craftsman,Villager,Commoner}` are filled (Lord/Knight/Steward/
Treasurer, blacksmiths + guard + merchant + townsfolk, elder/farmer/peasant, pilgrim).

## Guard clothing pack (2026-08)

`Entities/Clothing/guard.yml` — city-guard set, black-red livery from grayscale multi-layer art
(`Textures/_Respiral/Clothing/{Uniforms/Jumpsuit/guard_gambeson,Legs/guard_pants,Hands/guard_mittens}.rsi`).
Colors: body/black `#3E3838`, pattern/red `#9E2A2A`, codpiece linen `#C7C3B8`.
- **Gambeson** (jumpsuit) — one RSI, two prototypes (`...GuardGambesonCheckered` / `...Striped`): a shared
  grayscale body (`equipped-INNERCLOTHING`/`icon`/`inhand-*`) + a per-pattern overlay layer
  (`{checkered,striped}-equipped-INNERCLOTHING` etc). Two-tone: body tinted black, pattern tinted red on
  each of Sprite/Clothing/Item. `icon` state was extracted from the equipped south frame (source had no icon).
- **Pants** (`...LegsGuardPants`) — body + codpiece two-tone. Source exported as `equipped-INNERCLOTHING` →
  renamed `equipped-LEGS`. Single unisex `pants` key with two layers (body `equipped-LEGS` +
  codpiece `codpiece-equipped-LEGS`). Icon now uses two Sprite layers (`icon` + `codpiece-icon`) so the
  codpiece shows on the inventory icon too. No inhand art → `inhandVisuals:
  {left: [], right: []}` (icon fallback, see [[inhand_icon]]).
- **Mittens** (`...HandsGuardMittens`) — single-layer, `image.png` → `equipped-HAND`, tinted black.
- Armor via mixins ([[clothing]] armor rule): gambeson `MedievalArmorClothBody`, pants
  `MedievalArmorClothHeadOrLegs`, mittens `MedievalArmorClothOther`. Cloth (`MedievalFlax`/`MedievalCloth`).
- Recipes: `Recipe/guard_clothing.yml` (`MedievalBaseTailorClothingRecipe`, `MedievalTailor: Understanding`),
  registered in the `MedievalSewing` pack. `MedievalGuardGear` (city.yml) issues guard pants + mittens +
  chainmail + helmet.
- **Lobby loadout choice (checkered vs striped gambeson):** `_Respiral/Loadouts/guard.yml` — two `loadout`
  prototypes (`MedievalGuardGambeson{Checkered,Striped}`, each `equipment: {jumpsuit: <gambeson>}`), a
  `loadoutGroup MedievalGuardGambeson` (defaults MinLimit/MaxLimit = 1 → mandatory single pick, first =
  checkered), and a `roleLoadout` **`JobMedievalGuard`** (id = `"Job"` + jobId, see
  `Content.Shared/Clothing/LoadoutSystem.cs:GetJobPrototype`). The character-editor loadout tab shows the
  group for the guard job.
  - **The jumpsuit slot is deliberately absent from the player `MedievalGuardGear`** — a loadout can't swap a
    slot that startingGear already filled: `EquipStartingGear` uses `TryEquip(..., force: true)`, and force
    still fails on an *occupied* slot (container full), so the loadout item would just drop on the floor.
    Upstream jobs (e.g. QM) omit the customizable slot from startingGear for the same reason; the loadout
    group provides it (its MinLimit-1 default = checkered).
  - **NPCs don't get loadout customization** (RandomHumanoidSpawner → `EquipStartingGear` only, no
    `roleLoadout`). So `MedievalGuardGearNPC` (`parent: MedievalGuardGear`, adds `jumpsuit: ...Checkered` —
    `Equipment` is a merged dict) exists for the guard random spawner (`spawners.yml` `MedievalGuard`
    settings point at it), giving NPC guards the checkered gambeson.

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
- Third removal pass (2026-08): `MedievalClothingInnerLongTunic` and the whole Elite family
  (`EliteCloth/EliteHalf/EliteManjete/EliteMetal`) were deleted along with `inner.yml`, the
  `MedievalClothingInnerLongTunicRecipe` (recipe.yml + packs.yml), and all `jumpsuit:` lines
  referencing them in village.yml / nobles.yml. The **jumpsuit** slot now has no Respiral inner-clothing prototypes.
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

## Dresses (2026-08)

Six `outerClothing` dresses in `Entities/Clothing/outer/dress.yml`, from Twilight-Fortress
`OuterClothing/Dress/*.rsi` (CC-BY-SA-3.0): `emerald`, `sapphire`, `scarlet`, `white`,
`grayscale`, `grayscale2`. Prototypes: `MedievalClothingOuterDressEmerald/Sapphire/Scarlet/White/Gray/Simple`
(Gray = grayscale, Simple = grayscale2). Parent `[ MedievalBaseOuterClothing, MedievalArmorClothBody ]`,
cloth (`MedievalCloth: 100`), `damageModifierSet: MedievalFlax`.

Each .rsi has only `icon` + `equipped-OUTERCLOTHING` (no `inhand-*`). Since the base
`MedievalBaseOuterClothing` sets `Item.inhandVisuals` to `inhand-left/right`, each dress
**overrides `Item.inhandVisuals` to use `state: icon`** for both hands so the in-hand sprite renders.
This is the go-to pattern when an imported RSI lacks in-hand states.

Tailor recipes `MedievalClothingOuterDress*Recipe` (parent `MedievalBaseTailorClothingRecipe`,
`MedievalTailor: Confident`): coloured dresses cost `MedievalCloth: 150`, gray/simple `100`.
Registered in the `MedievalTailor` pack in `Recipe/packs.yml` (above the mantle recipes).

## Tailoring skill tiers & Royalty (2026-08)

`SkillKnowledgeLevel` ladder (`Content.Shared/_Respiral/Skill/SkillKnowledge.cs`):
Vague → Basic → Understanding → Confident → Deep → **Mastery** (Mastery is the ceiling, progress ≥ 1.0).
Tiers in use for `MedievalTailor`: `Understanding` (peasant/guard clothing, bookstand),
`Confident` (dresses), `Mastery` (Royalty, mantles, wallpaper/carpets/decor).

**Rule:** Royalty clothing must require a **higher** tailoring skill than ordinary garments. Mastery is the
cap → Royalty = `Mastery`; ordinary fancy clothing (dresses) = `Confident` so the gap is real.

Royalty prototypes in `Entities/Clothing/royalty.yml` (Twilight-Fortress, CC-BY-SA-3.0):
- `MedievalClothingOuterRoyalCassock` (`OuterClothing/Royalty/cassock.rsi`) — outerClothing, cloth 200.
- `MedievalClothingOuterRoyalDress` (`OuterClothing/Royalty/dress.rsi`) — outerClothing, cloth 200.
- `MedievalClothingOuterRoyalCoat` (`OuterClothing/Royalty/long_shirt.rsi`) — outerClothing, cloth 200.
- `MedievalClothingInnerRoyalShirt` (`Uniforms/Royalty/shirt.rsi`) — jumpsuit, cloth 150.

Each .rsi has `icon`/`colored-icon` + `equipped-*`/`colored-equipped-*`. Prototypes use the fixed-colour
`icon`/`equipped-*` states (the `colored-*` tintable variants are left unused — no colour spec). No inhand
states → `Item.inhandVisuals` overridden to `state: icon` (see [[inhand_icon]]). All cloth
(`MedievalArmorClothBody`, `MedievalFlax`). Recipes `MedievalClothingOuter/InnerRoyal*Recipe`
(`MedievalTailor: Mastery`) in the `MedievalTailor` pack.

## Material armor set (2026-08)

Four `outerClothing` armors appended to `Entities/Clothing/outer/armor.yml` (Twilight-Fortress, CC-BY-SA-3.0).
Per user: **no armor mixins** — parent is plain `MedievalBaseOuterClothing`, protection expressed only via a
suitable `damageModifierSet`. Skill scales with material (steel > leather > cloth); crafts use **multiple materials**.

| Prototype | RSI | damageModifierSet | Recipe skill (`MedievalBasicBlacksmithing`) | Materials |
|---|---|---|---|---|
| `MedievalClothingOuterArmorQuiltedGambeson` | quilted_gambeson | `MedievalFlax` | Understanding | Cloth 200 + Pelt 50 |
| `MedievalClothingOuterArmorLeatherJacket` | leather_jacket | `MedievalPelt` | Confident | Pelt 150 + Cloth 50 |
| `MedievalClothingOuterArmorPlate` | plate_armor | `MedievalSteel` | Deep | Steel 150 + Pelt 50 |
| `MedievalClothingOuterArmorPlateLeather` | plate_armor_leather_jacket | `MedievalSteel` | Mastery | Steel 150 + Pelt 100 |

Recipes parent `MedievalBaseMetalClothingRecipe` (category `MedievalMetalClothingCategory` — note
`MedievalMaterialArmorCategory` from `MedievalBaseArmorRecipe` is **undefined** in `category.yml`, so it's unused),
each overriding `neededSkills` for its tier. Registered in the **`MedievalAnvil`** pack (`# Броня` section),
so all four are crafted at the anvil like the rest of the armor. `plate_armor.rsi` **has** `inhand-left/right`
(no `Item` override); the other three lack inhand states → `inhandVisuals` set to `state: icon`.

Not yet prototyped: `Head/Hoods/{chainmail,chainmail_closed,cloth,quilted,quilted_closed}`, `Neck/chef_coat`,
`Tabards/*` (Tabards explicitly deferred by the user).
