# Skill System (skill progression)

## Architecture

- **SkillSystem** (`Content.Server/_Respiral/Skill/SkillSystem.cs`) — the main system: `AddSkillProgress`, `GetSkillProgress`, `CanLearn`
- **NeededSkillForMeleeSystem** (`Content.Server/_Respiral/Skill/NeededSkillForMeleeSystem.cs`) — a unified system for combat skills: damage modification + learning skills on hit
- **SkillPrototype** (`Content.Shared/_Respiral/Skills/Prototypes/SkillPrototype.cs`) — the skill prototype

## Components

### NeededSkillForMeleeComponent
Path: `Content.Server/_Respiral/Skill/Components/NeededSkillForMeleeComponent.cs`

| Field | Type | Description |
|---|---|---|
| `NeededSkillsProgress` | `Dictionary<ProtoId<SkillPrototype>, SkillKnowledgeLevel>` | Required weapon-skill knowledge level for **full** damage (mandatory) |
| `Points` | `List<Dictionary<ProtoId, float>>` | Points per hit by skill (mandatory) — unrelated to the gate, always a raw `float` learn-rate, NOT a `SkillKnowledgeLevel` |
| `LearnModifiers` | `List<SkillLearnModifier>` | Point multipliers by the target's whitelist; if the target matches none — no points are awarded |

Renamed from `NeededSkills` (a plain `List<ProtoId<SkillPrototype>>`) to `NeededSkillsProgress` (2026-08),
mirroring `NeededSkillForInteract{,Using}`. See "Knowledge levels" below for `SkillKnowledgeLevel`.

### SkillLearnModifier (`[DataDefinition]`)
Path: defined in `NeededSkillForMeleeComponent.cs`

| Field | Type | Description |
|---|---|---|
| `Whitelist` | `EntityWhitelist` | Target whitelist (mandatory) |
| `Modifier` | `float` | Point multiplier (mandatory) |

Modifiers stack multiplicatively.

## NeededSkillForMeleeSystem.OnNeededSkillForMeleeHit logic

1. **Damage** (2026-08 formula, per required skill in `NeededSkillsProgress`):
   ```csharp
   var required = SkillKnowledge.GetMinProgress(requiredLevel); // e.g. Mastery -> 1.0
   if (required <= 0f) continue;                                // Vague == no real requirement
   var actual = _skillSystem.GetSkillProgress(args.User, skill);
   damage *= Math.Min(required, actual / required);
   ```
   This is NOT gating — a hit always lands, only the damage multiplier scales. Picking `required = Mastery`
   (1.0) reproduces the old plain-linear behaviour (`damage *= actualProgress`), since
   `min(1, actual/1) == actual`. All existing weapon prototypes were migrated to `Mastery` for this reason
   (behaviour-preserving). Choosing a lower required level (e.g. `Confident`) lets the weapon reach full
   damage before full mastery of the skill — a tuning knob, not yet used by any prototype.
2. **Learning**: for each hit entity (unchanged):
   - Skip if hitting yourself
   - Compute the multiplier from `LearnModifiers` (summing matches); if 0 — skip
   - Award `value * modifier` points for each skill in `Points`

## YAML example

```yaml
- type: NeededSkillForMelee
  neededSkillsProgress:
    MedievalOneSword: Mastery
  points:
  - MedievalOneSword: 0.01
  learnModifiers:
  - whitelist:
      components:
      - MobState
    modifier: 2.0
```

## Crafting skills

| Skill ID | Name | Machine | Book |
|---|---|---|---|
| MedievalWoodProcessing | wood processing | MedievalSawmill, MedievalCarpenterTable | MedievalSkillBookWoodProcessing |
| MedievalStoneProcessing | stone processing | MedievalQuarry | MedievalSkillBookStoneProcessing |
| MedievalBasicBlacksmithing | blacksmithing | MedievalAnvil | MedievalSkillBookBasicBlacksmithing (+4 tiered volumes, see below) |
| MedievalTailor | tailoring | MedievalSewing, MedievalLoom | MedievalSkillBookTailor |
| MedievalAlchem | alchemy | MedievalAlchemTable | MedievalSkillBookAlchem |
| MedievalJewelry | jewelry | MedievalJeweler | MedievalSkillBookJewelry |

## Pattern for creating a new machine

1. Skill → `skill.yml` (type: skill, pointsNeeded: 100)
2. Tag → `tags.yml` (Medieval[Machine]MaterialStorage)
3. Category → `Recipe/category.yml` (type: latheCategory) + locale in `lathe/category.ftl`
4. Recipe pack → `Recipe/packs.yml` (type: latheRecipePack)
5. Machine detail → `Parts/production.yml` (parent: MedievalBaseMachineDetail, type: MachineBoard)
6. Machine entity → `Structures/Machine/lathe.yml` (parent: MedievalBaseLathe, NeededSkillForInteract)
7. Book → `Skill/book.yml` (parent: MedievalSkillBookBase, LearnSkillWhenUsing)
8. Tag on the input materials → add Medieval[Machine]MaterialStorage to the needed entities
9. Construction graph — shared MedievalMachine (no custom one needed)

## Skill-based usage gates

Two components (`Content.Server/_Respiral/Skill/`), both call `SkillSystem.CheckRequiredSkillsProgress`
(which ITSELF shows the popup `skill-need-skill-message` and returns false when lacking;
if the user has no `SkillComponent` at all — it's skipped, i.e. NPCs aren't blocked):

| Component | Event (engine) | What to attach to | What it blocks |
|---|---|---|---|
| `NeededSkillForInteract` | `BeforeInteractionActivate` + `InteractHandEvent` | the target (machine) | activation / hand-click on the object |
| `NeededSkillForInteractUsing` | `BeforeRangedInteractEvent` | the **used item** | applying the item TO something |

- **The YAML field for both — `neededSkillsProgress: { SkillId: Level, ... }`**, `Dictionary<ProtoId<SkillPrototype>,
  SkillKnowledgeLevel>` (`required: true`). Requires knowledge **≥ the given `SkillKnowledgeLevel`** per skill
  (converted to a progress threshold via `SkillKnowledge.GetMinProgress`, see "Knowledge levels" below).
  Machines previously required full mastery via a plain `neededSkills: [ ... ]` list + `CheckRequiredSkills`
  (2026-06/07) — **unified onto `NeededSkillsProgress`/`CheckRequiredSkillsProgress` in 2026-08** (all
  existing machine prototypes migrated to `Mastery`, which is behaviour-identical to the old full-mastery
  gate). `CheckRequiredSkills(List<ProtoId<SkillPrototype>>)` still exists but is now used ONLY by
  `CanLearn` (via `SkillPrototype.RequiredSkills` — skill-learning prerequisites, a different concept from
  usage gates) — don't confuse the two.
- **Examine description (ExaminedEvent, 2026-06):** all four skill components
  (`NeededSkillForInteract`, `NeededSkillForInteractUsing`, `NeededSkillForMelee`, `LearnSkillWhenUsing`)
  subscribe to `ExaminedEvent` and push a string via `args.PushMarkup`. Skill names are collected by the
  shared helper `SkillSystem.GetSkillNames(List<ProtoId<SkillPrototype>>)` (names from `SkillPrototype.Name`,
  comma-separated; the two `NeededSkillsProgress`-based components pass `.Keys` as that list).
  Locale `ru-RU/_respiral/skill.ftl`: `skill-exm-needed-skills` ("Using requires
  the following skills: …") for the three "needed" components,
  `skill-exm-learn-skills` ("Teaches the following skills: …") for the book (field `Skills`).
  There's no en-US version of skill.ftl — the original is ru only (as before).
- **`NeededSkillForInteractUsing` (2026-06):** it used to subscribe to a custom Respiral event
  `BeforeInteractUsingEvent`, which was raised NOWHERE (never existed in any commit) → the gate was
  dead. Switched to the engine `BeforeRangedInteractEvent` (`Content.Shared/Interaction/BeforeInteract.cs`,
  "raised directed on the used object when clicking on another object") — the exact built-in equivalent,
  requiring NO edits to upstream `SharedInteractionSystem`. The custom `Events/BeforeInteractUsing.cs` was removed.
  `Handled=true` on lacking a skill → `RangedInteractDoBefore` returns true → `InteractUsingEvent`
  on the target isn't raised (application blocked). Picking up/carrying the item is NOT blocked.

## LearnSkillWhenUsingComponent — tiered book learning (2026-08)

`Content.Server/_Respiral/Skill/Components/LearnSkillWhenUsingComponent.cs`. Used by skill books
(`Skill/book.yml`) and any other "use this to learn" item. Replaced the old flat `skills: [...]` list +
separate `points: { SkillId: float }` dict (which could desync — a skill in `skills` missing from `points`
threw `KeyNotFoundException` in `OnDoAfter`) with a single list of self-contained entries:

| Field (`LearnSkillEntry`, `[DataDefinition]`) | Type | Description |
|---|---|---|
| `Skill` | `ProtoId<SkillPrototype>` | Skill this entry teaches (mandatory) |
| `Points` | `float` | Progress granted per successful use (mandatory) |
| `MaxLevel` | `SkillKnowledgeLevel` | Ceiling this entry can raise the skill to (default `Mastery` = no cap, old behaviour) |
| `RequiredLevel` | `SkillKnowledgeLevel` | Minimum CURRENT level of the same skill needed to start learning via this entry (default `Vague` = always usable, old behaviour) |

`LearnSkillWhenUsingComponent.Skills` is now `List<LearnSkillEntry>` (YAML key unchanged: `skills:`, but
each item is now an object, not a bare skill id).

- **Why `RequiredLevel`+`MaxLevel` on the same skill:** models a tiered book series — "Volume 1"
  (`requiredLevel: Vague, maxLevel: Basic`) teaches a beginner up to Basic; "Volume 2"
  (`requiredLevel: Basic, maxLevel: Confident`) only works once you've already read Volume 1 to Basic,
  and caps at Confident.
- **First tiered series (2026-08): the 5 `MedievalBasicBlacksmithing` books** in `book.yml`, one per
  `SkillKnowledgeLevel` band (all bands are 0.2 wide, so `points: 0.01` per read = 20 reads/volume,
  matching the existing per-use rate convention):

  | Book id | Entity `name` | requiredLevel | maxLevel | Icon state |
  |---|---|---|---|---|
  | `MedievalSkillBookBasicBlacksmithing` (pre-existing id, kept for the trade-catalog reference) | "основы кузнечного дела" | — (Vague) | `Basic` | `anvil` |
  | `MedievalSkillBookBlacksmithingUnderstanding` | "кузнечное дело понимание" | `Basic` | `Understanding` | `book7` |
  | `MedievalSkillBookBlacksmithingConfident` | "Том 3" | `Understanding` | `Confident` | `book8` |
  | `MedievalSkillBookBlacksmithingDeep` | "Том 4" | `Confident` | `Deep` | `book9` |
  | `MedievalSkillBookBlacksmithingMastery` | "Том 5" | `Deep` | `Mastery` | `book10` |

  Naming (per requester's explicit choice, 2026-08): the first two volumes keep a wordy name (no colon,
  so no YAML quoting needed — a bare `name: кузнечное дело понимание` is a valid plain scalar); the last
  three are named by their position in the physical reading order ("Том 3/4/5", continuing from the
  conceptual "volumes 1–2" of Basic/Understanding), NOT by the order the three levels were mentioned in
  chat — numbering must follow the prerequisite chain (Confident→Deep→Mastery) or the labels would
  contradict the actual read order. All 5 are purchasable via matching `cargoProduct`s in
  `Catalog/medieval_trade.yml` (`MedievalTradeBookBlacksmithing{,Understanding,Confident,Deep,Mastery}`,
  escalating cost 1000→2000→3500→5500→8000; the shop display names for the last three also say
  "Том 3/4/5" to match the item's own name), with name/desc locale in BOTH `ru-RU` and `en-US`
  `cargo/medieval-trade.ftl` (`medieval-trade-product-book-blacksmithing{,-understanding,-confident,
  -deep,-mastery}{,-desc}`). All other skill books remain untiered (single entry, defaults, learn straight
  to Mastery) — this is a template for tiering other skills later, not applied elsewhere yet.
- **`LearnSkillWhenUsingSystem.CanLearnEntry(uid, entry)`** — the gate used by both `OnUseInHand` (decides
  whether to even start the DoAfter) and `OnDoAfter` (decides whether to actually grant points, entry by
  entry — previously it granted ALL entries unconditionally once the DoAfter completed, even ones the user
  could no longer learn). Combines `SkillSystem.CanLearn` (the unrelated cross-skill `RequiredSkills`
  prerequisite from `SkillPrototype`, plus a global "not already fully mastered" check) with
  `progress >= GetMinProgress(RequiredLevel) && progress < GetMinProgress(MaxLevel)`.
- **`SkillSystem.AddSkillProgress` gained an optional `maxProgress = 1f` parameter** (inserted between
  `progress` and `component`, both existing call sites are positional-only so this is non-breaking) —
  clamps `Math.Min(maxProgress, ...)` instead of the previous hardcoded `Math.Min(1f, ...)`. `OnDoAfter`
  passes `SkillKnowledge.GetMinProgress(entry.MaxLevel)`.
- **YAML migration:** `skills: [X]` + `points: { X: 0.01 }` → `skills: [{ skill: X, points: 0.01 }]` across
  all 8 entries in `book.yml` (`MaxLevel`/`RequiredLevel` left at defaults — behaviour-preserving).

## Per-recipe lathe skill gate (2026-08)

Skill requirement **per lathe recipe** (finer than the machine-level `NeededSkillForInteract`), with UI
feedback. Integration is deliberately minimal (chosen over forking the whole Lathe system): all new logic
lives in `_Respiral/` partial files, upstream is touched in only 3 marked spots.

- **`LatheRecipePrototype.NeededSkills`** (`Content.Shared/Research/Prototypes/LatheRecipePrototype.cs`,
  upstream file, `// Respiral`) — `Dictionary<ProtoId<SkillPrototype>, SkillKnowledgeLevel>?`. Nullable, so
  existing recipes are unaffected. It's an `IInheritingPrototype` and this is a dict, so it **merges down
  inheritance** — setting it on an abstract base recipe applies to all children.
- **`SkillRequirement.Meets(user, required, entMan)`** (`Content.Shared/_Respiral/Skill/SkillRequirement.cs`)
  — shared static, popup-free check (mirrors `CheckRequiredSkillsProgress` semantics: null/empty → true,
  no `SkillComponent` → true). Used by the client UI. The server reuses the popup-bearing
  `SkillSystem.CheckRequiredSkillsProgress` instead.
- **Server gate** — `Content.Server/_Respiral/Lathe/LatheSystem.Skill.cs` is a `partial class LatheSystem`
  (same class/assembly as upstream) adding `[Dependency] SkillSystem _skill` + `CheckRecipeSkill(user,
  recipe)`. Upstream `LatheSystem.OnLatheQueueRecipeMessage` calls it (`// Respiral`, 4 lines): on failure
  it refreshes the UI and returns without queueing. `args.Actor` = the player who clicked. Shows the
  `skill-need-skill-message` popup via the reused check.
- **Client UI** — `Content.Client/_Respiral/Lathe/LatheMenu.Skill.cs` is a `partial class LatheMenu` adding
  `[Dependency] IPlayerManager` + `MeetsRecipeSkill(recipe)` (local player) and `GetRecipeSkillTooltip(recipe)`
  (builds the missing-skill string, else null). Upstream `LatheMenu.PopulateRecipes` ANDs `canProduce` with
  `MeetsRecipeSkill` (greys the button), and `GenerateTooltipText` appends `GetRecipeSkillTooltip`
  (`// Respiral`, both minimal). Locale `skill-lathe-needed` / `skill-lathe-needed-entry` in ru-only
  `_respiral/skill.ftl` (uses `SkillKnowledge.GetLevelName` for the level word).
- **Content applied (blacksmithing, `Recipe/recipe.yml`):** `neededSkills` on the abstract bases →
  `MedievalBaseToolsRecipe: Basic`, `MedievalBaseWeaponRecipe: Understanding` (fallback default —
  overridden per-material on every concrete weapon recipe, see below), `MedievalBaseMetalClothingRecipe:
  Confident`. To make this reachable, the two blacksmithing machines' own `NeededSkillForInteract` gate
  (`MedievalSmelter`, `MedievalAnvil` in `Structures/Machine/lathe.yml`) was **lowered Mastery→Basic** —
  otherwise you couldn't open the anvil UI without full mastery, and the per-recipe tiers would be
  invisible. Machine gate = minimum to approach the forge; per-recipe = per-item tier.

### Per-material weapon tiers (2026-08)

All 80 concrete recipes parented to `MedievalBaseWeaponRecipe` got an explicit `neededSkills` override
(dict-merge on the same key replaces the abstract parent's value — see "Prototype Inheritance Rules" at
the top of this file), keyed off the material in the recipe id, using the price ordering from
`Entities/Objects/ProcessedMaterials/materials.yml` (Wood 1.41 < Iron 4.43 < Steel 10.76 < Silver 17.87 <
Mithril 43.02 < Adamantine 75.47) as the tiebreaker for materials the requester didn't name explicitly:

| Material | Level | Source |
|---|---|---|
| Wood, Pelt | `Vague` (no real gate) | inferred — cheapest/non-metal, no barrier |
| Iron | `Basic` | requester |
| Steel, Silver | `Understanding` | Steel: requester. Silver: inferred, price closer to Steel than Mithril |
| Mithril | `Confident` | requester |
| Adamantine | `Deep` | requester |
| Magic (Fire/Spectral Blade) | `Mastery` | requester |

Applies to every weapon family sharing this material set: BrassKnuckles, Cleaver, Dagger, Eveningstar,
Halberd, HideDagger, Knife, Mace, Polearm, Spear, Sword, Sword2, Whip (+ `PeltWhip`/`PeltWhip2`, no metal
variant). Generated via a Python regex pass over `recipe.yml` (id → material prefix → level), not
hand-edited — see git history for the exact mapping if re-deriving.

### Magic weapons made craftable (`Entities/Items/Weapon/magic.yml`, 2026-08)

`MedievalFireBlade`/`MedievalSpectralBlade` were previously loot/spawn-only (file header said "не
крафтятся"). Per requester's explicit instruction, added `MedievalFireBladeRecipe`/
`MedievalSpectralBladeRecipe` to `Recipe/recipe.yml` (parent `MedievalBaseWeaponRecipe`, `neededSkills:
{ MedievalBasicBlacksmithing: Mastery }`, materials `MedievalProcessedAdamantine: 200,
MedievalRawManaCrystal: 100` — matching each entity's own `PhysicalComposition`) and registered both in
the `MedievalAnvil` pack (`Recipe/packs.yml`). The file header comment in `magic.yml` was updated to
reflect they're now craftable at full mastery (loot/spawn still works too, unaffected). Both entities also
got a `PointLight` (requester follow-up) — Fire Blade `#E25822` (matches the torch fire color elsewhere in
`_Respiral`), Spectral Blade `#47F8FF` (matches the mana-fire/ice light color in `Structures/Light/light.yml`);
`radius: 2.5, energy: 2.5`, always-on (no `ItemTogglePointLight`/`enabled: false` — unlike fuel-based
torches/candles, these are permanently-enchanted blades, not something you light/extinguish).

### Machine gate fully retired in favour of per-recipe gating (2026-08)

Per requester's follow-up: **all** `NeededSkillForInteract` components were removed from the 10 lathe
machines in `Structures/Machine/lathe.yml` (Sawmill, Quarry, CarpenterTable, Smelter, Anvil, Tannery,
Loom, Sewing, Jeweler, PassportOffice) — opening any lathe UI no longer requires a skill at all; the gate
now lives entirely on the recipes. **`MedievalAlchemTable` was deliberately left untouched** — it has no
`Lathe` component (not part of this recipe system at all), so removing its gate would leave it completely
ungated with nothing to move the requirement to.

To backfill, **every concrete (non-abstract) `latheRecipe`** in `Recipe/recipe.yml` + `Recipe/carpets.yml`
(220 total) now has an effective `neededSkills`: the ~82 already tiered (80 weapons + 2 magic blades) were
left as-is; the other 138 got an explicit `{ skill: Mastery }` added directly on the concrete recipe as a
coarse placeholder ("на будущее — потом можно разбить по уровням, как оружие"), skill derived from
**which recipe-pack owns the recipe** (pack → machine → skill, same mapping the removed machine gates
used): `MedievalSawmillPack`/`MedievalCarpenterTable`→`MedievalWoodProcessing`,
`MedievalQuarryPack`→`MedievalStoneProcessing`, `MedievalSmelter`/`MedievalAnvil`/`MedievalAnvilCandel`
(unwired orphan pack, anvil-flavoured content)→`MedievalBasicBlacksmithing`,
`MedievalLoom`/`MedievalSewing`/`MedievalTannery`→`MedievalTailor`, `MedievalJeweler`
(currently `recipes: []`, no-op)→`MedievalJewelry`, `MedievalPassportOffice`→`MedievalBureaucracy`. One
recipe (`MedievalSmelterDetailRecipe`) isn't in any pack (orphaned/dead content) — assigned
`MedievalBasicBlacksmithing` by name. One recipe (`MedievalDrinkMugWoodRecipe`) is listed in TWO packs
(`MedievalCarpenterTable` and `MedievalAnvil`); resolved to `MedievalWoodProcessing` (first-pack-in-file
wins, and it's the semantically correct call — it's a **wood** mug regardless of which machine also
happens to offer it).

### Flat-Mastery placeholder walked back per material/complexity (2026-08, requester follow-up)

The bulk pass above stamped a flat `Mastery` on ~40 non-weapon recipes as a coarse placeholder — the
requester objected on the same logic already used for weapons (a hammer and a magic sword shouldn't need
the same mastery), so these were retiered instead of left at the placeholder:

- **Ingots** (`MedievalRawProcessingRecipe` children, smelter) — retiered by material using the SAME table
  as weapons (see "Per-material weapon tiers" above): Iron→`Basic`, Steel/Silver→`Understanding`,
  Gold→`Confident` (price 39.32, closer to Mithril's 43.02 than Silver's 17.87 — same tiebreaker rule),
  Mithril→`Confident`, Adamantine→`Deep`. Smelting ore into a basic ingot is arguably the most
  FOUNDATIONAL blacksmithing act, so it shouldn't gate above the equivalent-material weapon.
- **Candelabras** (`MedievalBaseLightingRecipe` children: `*Candelabra{1,3,Mini}`) + `MedievalSimpleMetalTorchRecipe`
  — retiered by material: Iron→`Basic`, Silver→`Understanding`, Gold→`Confident` (torch is iron-only→`Basic`).
- **`MedievalRoofHammerRecipe`/`MedievalHammerAmmoRecipe`** — simple tools/ammo, dropped to `Basic` (in line
  with the other `MedievalBaseToolsRecipe` children like keys/locks/scissors, which already inherited
  `Basic` untouched and were never part of the placeholder pass).
- **Left at `Mastery` on purpose**: the 5 machine-detail recipes (`MedievalSawmillDetailRecipe`,
  `MedievalQuarryDetailRecipe`, `MedievalJewelerDetailRecipe`, `MedievalSmelterDetailRecipe`,
  `MedievalPaperweightDetailRecipe`) — building a whole machine from scratch is a genuinely top-tier feat,
  distinct from forging a single item, so no per-material argument applies. `MedievalFireBladeRecipe`/
  `MedievalSpectralBladeRecipe` (magic weapons) also stay `Mastery` — that's the `Magic` row of the
  weapon-tier table, unrelated to this placeholder issue.

### Blacksmith job starting skill lowered (2026-08, same follow-up)

`Roles/Jobs/Craftsman/city.yml`, `MedievalBlacksmith` job: `MedievalBasicBlacksmithing` starting progress
`1` (full Mastery on spawn) → `0.5` (mid-`Understanding`). Rationale: skill books are tiered (5 volumes to
reach Mastery, see "tiered book learning" above) — a starting character having Mastery for free made the
whole tomes-and-progression system pointless for the one job built around this skill. `0.5` leaves a
professional blacksmith competent out of the gate (Basic/Understanding-tier: iron, most weapons and
tools) while `Confident`+ (Mithril, Gold, Adamantine, magic) still requires reading the higher-tier tomes
in-game. Every other Craftsman job in the same file also starts its trade skill at `1` — untouched here,
out of scope for this request (only blacksmith was named).

**⚠️ Inheritance pitfall hit and fixed while doing this:** the bulk pass initially checked only the raw
per-recipe YAML for an existing `neededSkills` key, missing that `MedievalBaseToolsRecipe` (`Basic`) and
`MedievalBaseMetalClothingRecipe` (`Confident`) already supply one **by inheritance** (dict-merge from the
abstract parent — see "Prototype Inheritance Rules" at the top of this file). That first pass wrongly
stamped an explicit `Mastery` override onto all 39 of their children (drinking vessels, keys, locks, coins,
belts/helmets/boots — none of which have their own per-material override), which either silently replaced
the inherited `Basic`/`Confident` with `Mastery` (same key) or, worse, merged in as a **second** key
alongside the inherited one — requiring BOTH skills to produce the item. Fixed by reverting the explicit
override on exactly those 39 (verified via a proper inheritance-resolving walk this time, not raw-YAML
`.get()`) so they go back to inheriting `Basic`/`Confident` from their abstract parent, untouched. **Lesson:
when checking "is X already specified" against `IInheritingPrototype` + dict fields, always resolve the
full parent chain — a raw per-entity YAML check will miss inherited values and can silently double- or
mis-gate content.**

Maps a raw skill progress 0..1 into a named "knowledge type" — used both in the skills UI (see
"Demonstration mode" below) and as the value type for every `NeededSkillsProgress` dictionary
(`NeededSkillForInteract`, `NeededSkillForInteractUsing`, `NeededSkillForMelee`).

- **`Content.Shared/_Respiral/Skill/SkillKnowledge.cs`** — `enum SkillKnowledgeLevel`
  (`Vague`/`Basic`/`Understanding`/`Confident`/`Deep`/`Mastery`) + static `SkillKnowledge`:
  `GetLevel(float)`, `GetMinProgress(level)` (inverse — the threshold a `NeededSkillsProgress` gate checks
  against), `GetLevelName(...)` (localized). Bands: `<0.2` Vague, `<0.4` Basic, `<0.6` Understanding,
  `<0.8` Confident, `<1.0` Deep, `>=1.0` Mastery. Locale keys
  `skill-knowledge-{vague,basic,understanding,confident,deep,mastery}` in `ru-RU/_respiral/skill.ftl`
  (skill.ftl is ru-only). Plain (non-`[Flags]`) enum — serializes in YAML by bare member name (e.g. `Mastery`),
  same convention as any other simple engine enum (cf. `MobState`'s `Alive`/`Dead`/…).
- **`SkillSystem.CheckRequiredSkillsProgress(EntityUid, Dictionary<ProtoId<SkillPrototype>,
  SkillKnowledgeLevel>)`** — converts each required level via `GetMinProgress` and compares against
  `GetSkillProgress`; same popup/NPC-skip semantics as before, only the dictionary value type changed
  (was `float` progress 0..1 pre-2026-08).

### Demonstration mode — evaluability gate
- **`SkillInfo`** gained `bool Evaluable = true` (positional record-struct, mutable) — whether the
  viewer may assess the skill. Networked (used only via the share EUI).
- **Server `SkillShareSystem.BuildSkillList(target, viewer, targetComp)`** — both share paths use it.
  Per skill `Evaluable = viewerProgress >= targetProgress` (viewer must know it at least as well as the
  demonstrator, else the exact level is hidden). `viewer == null` → all evaluable (admin-ghost path).
  A viewer without `SkillComponent` reads 0 for every skill → can't evaluate anything the target has > 0.
- **Client `SkillsListWindow.SetSkills(..., bool demonstration = false)`**:
  - `demonstration == false` (own skills, character-menu path) → right label shows the exact `N%` (unchanged).
  - `demonstration == true` (share EUI, `SkillsListEui` passes it) → right label shows
    `SkillKnowledge.GetLevelName(Progress)`; if `!Evaluable` the right label is blank, the description is
    replaced by `skill-knowledge-cant-evaluate`, and the progress bar is forced full (real level not leaked).

## Paper press + scribe's desk (MedievalPaperweight / MedievalBookstand, 2026-08)

Two new lathes, following the standard "Pattern for creating a new machine" above:

- **`MedievalPaperweight`** (`Structures/Machine/lathe.yml`) — consumes `MedievalProcessedWood` (100),
  produces `MedievalPaper` (`Recipe/paperweight.yml`, `MedievalPaperRecipe`). No skill gate — anyone can
  press paper. Tag `MedievalPaperweightMaterialStorage` added onto `MedievalBoard`
  (`Entities/Objects/ProcessedMaterials/production.yml`) so boards/wood feed the press.
- **`MedievalBookstand`** ("стол писца") — consumes `MedievalPaper` (300) to write any of the 11 skill
  books from `Skill/book.yml` (`Recipe/bookstand.yml`). Tag `MedievalBookstandMaterialStorage` added onto
  `MedievalBasePaper` (`Entities/Objects/Bureaucracy/paper.yml`) so any paper-based item feeds it.
- **Per-book `neededSkills`** (dict-merge inheritance, see "Prototype Inheritance Rules"): the abstract
  `MedievalBaseBookstandRecipe` sets `MedievalLiteracy: Understanding` once; each concrete recipe adds
  only ITS subject skill, at the level the book teaches UP TO (you must already know the material to
  write it down) — e.g. the 5 tiered blacksmithing volumes require `Basic/Understanding/Confident/Deep/
  Mastery` respectively, matching each volume's own `maxLevel` in `book.yml`. The literacy primer
  (`MedievalSkillBookLiteracyRecipe`) overrides the inherited literacy key down to `Vague` (no real gate,
  see [[skill_system.md]] "Knowledge levels") — otherwise writing the FIRST literacy primer would need
  literacy itself.
- **"A lot of time"** (requester's phrase) — `completetime` starts at 600s (10 min) for untiered books,
  scaling up per blacksmithing volume (600/750/900/1050/1200) to mirror the skill-level climb; the literacy
  primer is shorter (300s) as the introductory book.
- **Bootstrapping the machines**: `MedievalPaperweightDetail`/`MedievalBookstandDetail`
  (`Objects/Parts/production.yml`, `MachineBoard` prototype = the lathe id).
  `MedievalBookstandDetailRecipe` is craftable at the `MedievalCarpenterTable`
  (`neededSkills: { MedievalWoodProcessing: Mastery }`, 300 wood) — same pattern as
  `MedievalPassportOfficeDetailRecipe`. `MedievalPaperweightDetailRecipe` (requester follow-up) is instead
  forged on the `MedievalAnvil` pack (`neededSkills: { MedievalBasicBlacksmithing: Mastery }`, 200 wood +
  200 iron) — matching the other machine-detail recipes on that pack (`MedievalSawmillDetailRecipe`,
  `MedievalQuarryDetailRecipe`, `MedievalJewelerDetailRecipe`), which are all blacksmithing-gated
  regardless of the target machine's own trade.

## Literacy (MedievalLiteracy)

- The `MedievalLiteracy` skill (`skill.yml`, `pointsNeeded: 100`, mastery = progress ≥ 1.0).
- **Shared helper `Content.Shared/_Respiral/Skill/Literacy.cs`** (static, 2026-07): single source for the
  skill ProtoId `Literacy.Skill = "MedievalLiteracy"` + `Literacy.GetLevel(EntityUid, IEntityManager)` →
  level 0..1 (resolves `SkillComponent` itself, like `Identity.Name`; no comp → `1f` = fully literate,
  e.g. NPCs/ghosts; comp without the skill → `0f`). Consumers:
  `PaperWindow.GetLocalLiteracy` (reading scramble) and `MedievalIdentitySystem` (passport "present to one"
  gate). Replaced the two duplicated `"MedievalLiteracy"` literals.
- The book `MedievalSkillBookLiteracy` (`book.yml`, icon `book5`, `LearnSkillWhenUsing` 0.01/use → 100 reads).
- `MedievalPen` carries `NeededSkillForInteractUsing` → without enough literacy you can't write with the
  pen (apply it on paper/anything): `neededSkillsProgress: { MedievalLiteracy: Deep }` (`Deep`'s min
  progress is exactly 0.8 — same 80%-literacy threshold as the old float value, just re-expressed as an
  enum level after the 2026-08 migration).

### Reading paper by literacy (character substitution, 2026-06)

The paper text is displayed as "scribbles", more so the lower the READER's literacy.

- **Why on the client, not the server:** `PaperSystem` (Shared) sends the text via
  `SharedUserInterfaceSystem.SetUiState` — **one state for all viewers** (there's NO per-session
  variant in the engine). The server can't show one paper to two readers differently. So the
  substitution is on the client at render time, based on the local `SkillComponent` (it's `[NetworkedComponent]`,
  the client knows its own level). Tradeoff: the raw text still goes to the client (normal for SS14).
- **Algorithm:** `Content.Shared/_Respiral/Skill/LiteracyScramble.cs` —
  `Apply(string markup, float progress)`. The fraction of substituted letters ≈ `(1 - progress)`. Deterministic
  (FNV-1a seed by text → no flicker), **monotonic** (literacy growth only reveals letters),
  substitutes letters with random ones of the same alphabet/case (Cyrillic/Latin), doesn't touch spaces/punctuation/
  digits/`[...]` tags.
- **Hook:** `Content.Client/Paper/UI/PaperWindow.xaml.cs`, `Populate()` — Read mode only
  (`isEditing == false`); the input field is not scrambled. The local player's literacy is taken in
  `GetLocalLiteracy()` (`IPlayerManager.LocalEntity` → `Literacy.GetLevel(SkillComponent?)`, see the
  shared helper above; no entity → reads everything). Edits marked `// Respiral`.
- **Reactivity v1:** the picture updates when the UI state updates; raised literacy with the paper open
  → reopen. Auto-refresh on `SkillComponent` change — not done.
- The old `_Respiral/Illiteracy` (server-side `ScrambleText` in `PaperSystem`) was reverted to upstream earlier;
  this is a NEW, separate client-side implementation (no more binary "literate/not" — it's a gradient).

### Viewing skills (client UI)

- **Own skills — character menu button.** Client `SkillSystem`
  (`Content.Client/_Respiral/Skill/SkillSystem.cs`) hooks the upstream character menu via
  `CharacterInfoSystem.GetCharacterInfoControlsEvent` (raised from
  `Content.Client/CharacterInfo/CharacterInfoSystem.cs:GetCharacterInfoControls`, added to the Objectives
  box in `CharacterUIController.CharacterUpdated`). It adds a "Навыки" button (`skill-character-button`)
  that opens `SkillsListWindow` for the local player. **The button is always shown** (2026-07): the old
  `_currentSkills.Count == 0` early-returns (both in the add-button handler and the press handler) were
  removed — with 0 skills the window opens showing `skills-list-no-skills`. `SkillsListWindow.SetSkills`
  already handles the empty list.
- **Other players' skills** — via the `SkillComponent` verb (`SkillShareSystem`, server): asks the target
  a Yes/No via `ConfirmDialogSystem.OpenConfirmDialog` (see below); on "Yes" the server calls
  `ShowSkillsTo(target, requester)` which re-checks distance and opens `SkillsListEui` for the requester.
  Admin-ghost opens directly (`OpenSkillsDirectly`). The verb early-returns when `User == Target`, so it is
  NOT the path for own skills (that's the character button). **The old per-use-case
  `SkillShareRequestEui` (client+server) + `SkillShareRequestEuiState`/`SkillShareResponseMessage` were
  removed** in favour of the shared dialog system.

### Reusable Yes/No dialog — `ConfirmDialogSystem` (2026-08)
Generic confirmation dialog modelled on upstream `QuickDialogSystem.OpenDialog`, but for a simple
да/нет choice handled via callbacks (no bespoke EUI per feature). Lives in `_Respiral/UI`:
- **Shared** `Content.Shared/_Respiral/UI/ConfirmDialogEvents.cs` — networked `ConfirmDialogOpenEvent`
  (id, title, message, yesText, noText) + `ConfirmDialogResponseEvent` (id, confirmed).
- **Server** `Content.Server/_Respiral/UI/ConfirmDialogSystem.cs` —
  `OpenConfirmDialog(session, title, message, yesAction, noAction? = null, yesText? = null, noText? = null)`.
  Tracks open dialogs by an incrementing id + per-`NetUserId` list (same bookkeeping as QuickDialog);
  `OnResponse` invokes `yesAction`/`noAction` and disconnects on an unknown id; disconnect/zombie →
  `noAction` for all the user's open dialogs. Default button captions from locale `confirm-dialog-{yes,no}`
  (`ru-RU/_respiral/ui.ftl`).
- **Client** `Content.Client/_Respiral/UI/ConfirmDialogSystem.cs` + `ConfirmDialogWindow.cs` — opens the
  window on the open-event, raises the response on Yes/No; closing the window without a choice = No
  (guarded by an `answered` flag so the explicit Yes/No close doesn't double-send).
- **Usage pattern:** inject the server system, call `OpenConfirmDialog(...)` with a `yesAction` lambda.
  Reuse this for any future да/нет prompt instead of a new EUI. skill-share locale keys
  (`skill-share-verb-text`, `-verb-admin-text`, `-request-title`, `-request-message`) were also added to
  `skill.ftl` (they were previously undefined → raw keys shown).

### Console command: setskill (2026-07, renamed from `addskill` 2026-08)

`Content.Server/_Respiral/Commands/SetSkillCommand.cs`, `[AdminCommand(AdminFlags.Debug)]`.
Usage: `setskill <target|self> <skillId> [progress]`. **Sets** an absolute progress (can lower it,
even to 0 — unlike `SkillSystem.AddSkillProgress` which only increments/clamps up); the "set" semantics
are why it isn't built on `AddSkillProgress`. Named `setskill` to match that.
- `target`: a NetEntity, or the literal `self` (the running player's attached entity).
- `progress`: optional float 0..1 (default 1 = fully learned), clamped.
- `EnsureComponent<SkillComponent>` on the target (works on entities without one), sets
  `Skills[skillId] = progress`, then `Dirty` so it networks to the client (needed for the character-menu
  skills button to reflect it). Tab-completion lists all `SkillPrototype` IDs on arg 2.

### Guidebook
The "Skills" tab (`MedievalSkills`) — child of the root `Medieval` (see identity.md about the guidebook
structure). `Prototypes/_Respiral/Guidebook/guidebook.yml`: `MedievalSkills` (Skills.xml, overview) +
child `MedievalSkillsLiteracy` (SkillsLiteracy.xml, literacy: learning from a primer, gradient reading,
writing with a pen). XML in `ServerInfo/Guidebook/_Respiral/`, tab names in
`Locale/{ru-RU,en-US}/_Respiral/guidebook.ftl` (`guide-entry-medieval-skills{,-literacy}`).
Embed entities: `MedievalSkillBookLiteracy`, `MedievalPen`.

## Engineering skill + build hammer (2026-08)

- **`MedievalEngineering`** skill (`skill.yml`, `pointsNeeded: 100`) — construction skill for the
  build hammer.
- **`MedievalHammer`** (`Entities/Items/Tools/tool.yml`) is now a pure construction tool
  (`Tool` quality `MedievalScore`); its **RCD was removed** (deleted the `UserInterface`
  `RcdUiKey`/`RCDMenuBoundUserInterface` + `ActivatableUI` blocks, and the `RCD` entry from
  `AddWearAfterUse.triggers`). Structures are built via the Tool quality on construction graphs.
- **Skill gate:** `- type: NeededSkillForInteractUsing` with `neededSkillsProgress: { MedievalEngineering: Mastery }`.
  Works because applying the hammer to a construction ghost/frame is an `InteractUsing`, which raises
  `BeforeRangedInteractEvent` on the used hammer BEFORE the target's `InteractUsingEvent` (construction).
  Gate handles it → build blocked; skipped for entities without `SkillComponent` (NPCs).
- The **Engineer job** carries `MedievalEngineering: 1` (see [[professions.md]]) so it can build.
- **Tool wear on construction** — new `WearTrigger.Tool` (`1 << 7`) in `AddWearAfterUseComponent`.
  `AddWearAfterUseSystem` subscribes to `ToolUsingFinishedEvent` (the empty `[ByRefEvent]` in
  upstream `SharedToolSystem.OnDoAfter`, marked `// Respiral-Wear`, previously raised but UNSUBSCRIBED).
  Every tool DoAfter (construction/pry/anchor/etc.) raises it on the tool → handler adds wear when the
  `Tool` flag is set. `ToolUsingFinishedEvent` was extended with a `bool Cancelled` (fed from
  `args.Cancelled`) so an interrupted DoAfter doesn't wear the tool (mirrors the `Learn` guard). Hammer
  triggers = `[MeleeHit, Tool]`. See [[upstream_patches.md]] for the `SharedToolSystem` patch.

## Skill-granted components (`grantComponents`, 2026-08)

A skill can add/remove components on its owner based on mastery — the mechanism for turning "flavour"
skills into real capabilities.

- **`SkillPrototype.GrantComponents`** — `Dictionary<float, ComponentRegistry>` (key = progress
  threshold 0..1, value = component set). Progress ≥ threshold → the components are granted; below →
  removed. Multiple thresholds = tiered unlocks (e.g. 0.5 → A, 1.0 → A+B). YAML:
  ```yaml
  grantComponents:
    1:
    - type: PriceAppraisal
  ```
- **Grant/revoke lives in server `SkillSystem`** (`RefreshSkillGrants(uid)` iterates all skills →
  `UpdateSkillGrant` per skill). Called from: `OnInit` (ComponentInit — covers job `AddComponentSpecial`
  spawns), `AddSkillProgress` (per-skill, after `Dirty`), `OnCloning`/`OnPolymorphed` (after copying
  Skills), and the `setskill` command (via `_entManager.System<SkillSystem>().RefreshSkillGrants`).
- **Idempotency is required** (RefreshSkillGrants runs on every progress tick): uses
  `EntityManager.AddComponents(uid, registry, removeExisting: false)` — already-present components are
  **skipped** (no overwrite, no field reset), and `RemoveComponents` no-ops if absent. Never pass
  `removeExisting: true` here or each tick would wipe the component's runtime state.
- **Granted components must be SHARED** (`[RegisterComponent]` in `Content.Shared`), because
  `SkillPrototype` is a shared prototype — its `ComponentRegistry` is validated on BOTH client and
  server (YAMLLinter). A server-only component name would fail client prototype load.

### Merchant price appraisal (first use case)

- Skill **`MedievalTrading`** (`skill.yml`, "торговое дело") grants `PriceAppraisalComponent` at
  progress 1. The **Merchant** job carries `MedievalTrading: 1` (+ Literacy). See [[professions.md]].
  Learnable like other crafts: book **`MedievalSkillBookTrading`** (`book.yml`, icon `book6`,
  `LearnSkillWhenUsing` +0.01/use), sold in the market as `MedievalTradeBookTrading`
  (`Catalog/medieval_trade.yml`, cost 1000, locale `medieval-trade-product-book-trading{,-desc}` in
  BOTH ru-RU + en-US `cargo/medieval-trade.ftl`).
- **`PriceAppraisalComponent`** — shared marker (`Content.Shared/_Respiral/Trade/Components/`).
- **Display is on the PRICED entity, not the merchant.** `MedievalPhysicalPricedSystem` (server,
  `_Respiral/Pricing/`) subscribes to `ExaminedEvent` on `MedievalPhysicalPricedComponent`; if
  `args.Examiner` has `PriceAppraisalComponent` it `PushMarkup`s the price (`_pricing.GetPrice(ent)`,
  locale `trade-appraisal-price` in `ru-RU/_respiral/trade.ftl`). Rationale: `ExaminedEvent` is directed
  at the examined target, so a component on the examiner can't receive it — the target checks the
  examiner instead (the approach the requester suggested).

## Bugfixes (2026-07)

- **`AddSkillProgress` didn't network.** `SkillComponent.Skills` is an `AutoNetworkedField`, but
  `SkillSystem.AddSkillProgress` (server) mutated the dictionary WITHOUT `Dirty` → the client never got
  the update, so a skill learned from a book (e.g. `MedievalLiteracy`) never appeared in the character-menu
  skills list even with progress > 0. Fixed: `Dirty(uid, component)` at the end of `AddSkillProgress`.
- **Skills-list window title untranslated.** `skills-list-window-title` (and `skills-list-no-skills`)
  were missing from `Locale/ru-RU/_respiral/skill.ftl` → raw key shown. Added ("Навыки").
- **Skills-list icons too small.** `SkillsListWindow.SetSkills` rendered at `skill.IconSize` (default 16px).
  Now `Math.Max(skill.IconSize, 32)` — min 32px display (explicit larger sizes still respected).
- **Skill books took no wear on reading.** `AddWearAfterUseSystem.OnLearnDoAfter` (the `WearTrigger.Learn`
  handler) + its `LearnDoAfterEvent` subscription + the `Content.Shared._Respiral.Skills.Events` import were
  all commented out → reading a book (`AddWearAfterUse triggers: [Learn, MeleeHit]`) never applied its
  `Slash: 1` damage, so the book never degraded. Re-enabled; added an `args.Cancelled` guard so an
  interrupted read (BreakOnMove/BreakOnDamage) doesn't damage the book.
- **Wear examine showed a huge "до поломки" number.** `WearSystem.OnExamine` reads a `Breakage` threshold,
  but NO Respiral Wear item defines one (weapons/tools/books all use only `Destruction`) →
  `GetThresholdAt(Breakage)` returns `FixedPoint2.MaxValue`. Fixed: when there's no breakage threshold
  (`breakThreshold == FixedPoint2.MaxValue`), show only the destroy durability (`wear-exm-destroy-info`)
  instead of the break/destroy pair. The "N до разрушения" from the `Destruction` threshold (books: 100)
  is the real durability.

## History

- `LearnSkillWhenMeleeAttackSystem` and `LearnSkillWhenMeleeAttackComponent` — removed, functionality moved into `NeededSkillForMeleeSystem` and `NeededSkillForMeleeComponent`
