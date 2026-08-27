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
| MedievalCraftsman | Blacksmith, Carpenter, Mason, Engineer, Innkeeper, Herbalist, Tailor, Citizen, Merchant, Guard |
| MedievalVillager | Elder, Farmer, Peasant |
| MedievalCommoner (weight -10) | Refugee |

Supervisor chain: Lord → Knight/CitySteward/Treasurer/Elder; CitySteward → all craftsmen;
Elder → Farmer/Peasant; authority → Refugee.

## Jobs = starting gear + starting skills (2026-08)

Each `type: job` references a `startingGear` that grants clothing (mostly hood + leather pants).
Spawners (`Roles/spawners.yml`) create `RandomHumanoidSpawner` + `Loadout` NPC variants of every
job (suffix "Средневековье").

**Starting skills** are granted on spawn via `special: - !type:AddComponentSpecial` adding
`- type: Skill` (`SkillComponent`, `skills: { SkillId: progress }`). All at progress `1` (full
mastery) so a professional can immediately use their gated machine (`NeededSkillForInteract` +
`CheckRequiredSkills` needs ≥ 1.0). `SkillComponent.Skills` is an `AutoNetworkedField`, so the
YAML-set values network to the client on spawn automatically.

| Job | Starting skill(s) |
|---|---|
| Blacksmith | BasicBlacksmithing |
| Carpenter | WoodProcessing |
| Mason | StoneProcessing |
| Engineer | Engineering + WoodProcessing + StoneProcessing |
| Innkeeper, Herbalist | Alchem |
| Tailor | Tailor (Tannery/Loom/Sewing machines) |
| Merchant | Trading (→ price appraisal on examine) + Literacy |
| CitySteward | Literacy + Bureaucracy (→ Passport Office) |
| Elder | Literacy |
| Guard | OneSword + Mace |
| Lord | Literacy + OneSword |
| Knight | OneSword + AncientWeapons |
| Treasurer | Literacy + Jewelry |
| Citizen, Peasant, Farmer, Refugee | FistFight (commoner default — no craft skill fits the role) |

Files: `Roles/Jobs/{Craftsman/city,Nobles/nobles,Villager/village,Commoner/refugee}.yml`.
NPC spawner variants don't get a `SkillComponent` (they use the job's gear loadout, not `special`),
so NPCs still bypass skill gates by design.

### Adding a new job — the 5 coordinated files (Tailor added 2026-08)

`MedievalTailor` (портной, owns the `MedievalTailor` skill → Tannery/Loom/Sewing) was added as the
worked example. A new job needs edits in **all** of:
1. `Roles/Jobs/<dept>/*.yml` — the `type: job` (+ its `type: startingGear`), with the `Skill` special.
2. `Roles/Jobs/departments.yml` — add the job id to the department's `roles:` list (else it never
   appears in lobby).
3. `Roles/play_time_trackers.yml` — a `type: playTimeTracker` matching the job's `playTimeTracker:`.
4. `Locale/ru-RU/_respiral/job/job.ftl` — `job-name-…` + `job-description-…` (ru-only; there is NO
   en-US job.ftl, matching the existing convention).
5. `Roles/spawners.yml` — `MedievalBaseHumanoidSpawner` entity + `randomHumanoidSettings` (id = job
   id, `Loadout.prototypes: [ <Gear> ]`) for the NPC variant.

Note: the `job`, the `randomHumanoidSettings`, and the `skill` can all share the id `MedievalTailor`
— different prototype types don't collide. Job icon `JobIconServiceWorker` (no medieval-specific
tailor icon exists; reuses an upstream `jobIcon`).

## Skills (`Skill/skill.yml`, all pointsNeeded: 100)

Craft (learned via books, `LearnSkillWhenUsing` +0.01/use → 100 uses to master):
WoodProcessing, StoneProcessing, BasicBlacksmithing, Tailor, Alchem, Jewelry, Literacy.
Bureaucracy exists as a skill (CitySteward-only, gates the Passport Office) but has **no book yet**.
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
| MedievalJeweler | Jewelry | ⚠️ **empty pack** — sprite `jewelry.rsi`, detail wood+steel (Anvil), accepts raw/processed gold+silver & all 4 gems; no craft recipes yet |
| MedievalPassportOffice | Bureaucracy | passport (consumes `MedievalPaper` material) |
| MedievalAlchemTable | Alchem | ChemMaster UI (pills/bottles) |
| MedievalHotplate | none | SolutionHeater 160/s |
| MedievalReagentGrinder | none | ReagentGrinder |
| MedievalConnectingMagicBall | none | CommunicationsConsole (announcements) |

Machine boards/details are `MachineBoard` entities in `Entities/Objects/Parts/production.yml`,
crafted at CarpenterTable/Anvil (`MedievalCarpenterTableDetailRecipe` etc.).

**⚠️ Lathe material-whitelist gotcha:** a `Lathe` auto-derives its accepted materials from its
recipes (`LatheSystem.OnGetWhitelist` → overwrites `MaterialStorage.MaterialWhiteList` on MapInit).
An **empty pack ⇒ empty material whitelist ⇒ the machine accepts NO material inserts**, regardless of
the entity `Whitelist` tags. So `MedievalJeweler` (empty pack) won't accept gold/silver/gems until it
has ≥1 recipe — the storage tags are scaffolding that activates once recipes exist. Setting
`MaterialWhiteList` in YAML does not help (it's overwritten at init).

## Known gaps / balance notes

1. ~~Job grants no skill~~ **RESOLVED (2026-08):** every job now grants a starting `SkillComponent`
   via `AddComponentSpecial` (see "Jobs = starting gear + starting skills" above). Books still let
   anyone learn any craft from zero — starting skills just give professionals a head start at mastery.
2. **MedievalJeweler empty pack** + no Jeweler job in any department (skill exists, role doesn't) —
   Treasurer now carries `MedievalJewelry`, but the Jeweler machine's pack is still empty.
3. Commoner roles (Citizen, Peasant, Farmer, Refugee) have no craft skill that fits → all given
   `MedievalFistFight` as a placeholder. Farmer has no farming skill in `skill.yml` at all.
4. Combat skills have no training books (melee-only); `MedievalAncientWeapons` name typo ("древкое").
5. Alchemy only partially gated (Table gated; Grinder + Hotplate open to all).
6. NPCs bypass all skill gates by design (no SkillComponent).
