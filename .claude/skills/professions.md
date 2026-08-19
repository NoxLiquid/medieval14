# Professions, Departments & Machines (roles map)

Maps the three prototype groups together: `Roles/` (jobs/departments/spawners), `Skill/`
(skills + books), `Entities/Structures/Machine/` (lathes gated by skills). See also
[[skill_system.md]], [[economy.md]], [[trade_console.md]], [[communications.md]].

Human-facing design doc (RU): separate mdBook repo `respiral-docs`
(`/home/user/workspace/respiral-docs`, https://github.com/NoxLiquid/respiral-docs) → GitHub Pages.
Content split across `src/professions/{overview,design-plan}.md` and `src/departments/*.md`.

## Departments (`Roles/Jobs/departments.yml`)

| Department id | Roles |
|---|---|
| MedievalNobles | Lord, Knight, CitySteward, Treasurer |
| MedievalCraftsman | Blacksmith, Carpenter, Mason, Engineer, Innkeeper, Herbalist, Citizen, Merchant, Guard |
| MedievalVillager | Elder, Farmer, Peasant |
| MedievalCommoner (weight -10) | Refugee |

Supervisor chain: Lord → Knight/CitySteward/Treasurer/Elder; CitySteward → all craftsmen;
Elder → Farmer/Peasant; authority → Refugee.

## Jobs = starting gear only

Each `type: job` references a `startingGear` that grants ONLY clothing (mostly hood + leather
pants). **No job grants a skill, book, or machine.** Spawners (`Roles/spawners.yml`) create
`RandomHumanoidSpawner` + `Loadout` NPC variants of every job (suffix "Средневековье").

## Skills (`Skill/skill.yml`, all pointsNeeded: 100)

Craft (learned via books, `LearnSkillWhenUsing` +0.01/use → 100 uses to master):
WoodProcessing, StoneProcessing, BasicBlacksmithing, Tailor, Alchem, Jewelry, Literacy.
Combat (learned in melee, no books): OneSword, Dagger, Mace, FistFight, Whip, AncientWeapons.
Books in `Skill/book.yml` (parent `MedievalSkillBookBase`); no book for Jewelry-vs... actually
Jewelry HAS a book (`MedievalSkillBookJewelry`); combat skills have none.

## Machine → skill gate → recipe pack (`Structures/Machine/`, `Recipe/packs.yml`)

Gate = `NeededSkillForInteract` (skipped for entities without `SkillComponent`, i.e. NPCs).
Base chain: `MedievalBaseMachine` → `MedievalMachine` → `MedievalBaseLathe`. Construction graph
`MedievalMachine` (frame → machineFrame → machine). Each lathe has a `MaterialStorage` whitelist
tag `Medieval<Name>MaterialStorage`.

| Machine | Skill gate | Output pack |
|---|---|---|
| MedievalSawmill | WoodProcessing | boards, wood rods |
| MedievalCarpenterTable | WoodProcessing | buckets, mops, mugs, **machine details**, wood floor tiles |
| MedievalQuarry | StoneProcessing | stone/marble floor tiles |
| MedievalSmelter | BasicBlacksmithing | ingots (iron→adamantine), bottles |
| MedievalAnvil | BasicBlacksmithing | weapons, armor, tools, keys/locks, coins, mugs, candelabra |
| MedievalTannery | Tailor | processed pelt |
| MedievalLoom | Tailor | cloth |
| MedievalSewing | Tailor | clothes, bags, belts, bedsheets, carpets |
| MedievalJeweler | Jewelry | ⚠️ **empty pack** (`recipes: []`) — non-functional |
| MedievalAlchemTable | Alchem | ChemMaster UI (pills/bottles) |
| MedievalHotplate | none | SolutionHeater 160/s |
| MedievalReagentGrinder | none | ReagentGrinder |
| MedievalConnectingMagicBall | none | CommunicationsConsole (announcements) |

Machine boards/details are `MachineBoard` entities in `Entities/Objects/Parts/production.yml`,
crafted at CarpenterTable/Anvil (`MedievalCarpenterTableDetailRecipe` etc.).

## Known gaps / balance notes

1. **Job grants no skill** → roles are cosmetic; anyone with a book becomes any craftsman.
   Design decision pending: grant starting skill/book per job, or keep "everyone learns from zero".
2. **MedievalJeweler empty pack** + no Jeweler job in any department (skill exists, role doesn't).
3. Jobs with no gameplay loop/skill: Engineer, Innkeeper, Citizen, Guard, Peasant, all Nobles.
4. Combat skills have no training books (melee-only); `MedievalAncientWeapons` name typo ("древкое").
5. Alchemy only partially gated (Table gated; Grinder + Hotplate open to all).
6. NPCs bypass all skill gates by design (no SkillComponent).
