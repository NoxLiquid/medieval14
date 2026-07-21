# Mobs — NPCs and monsters

## Mob architecture

Mobs inherit from the SS14 base prototype chain:
`BaseMob` → `BaseSimpleMob` → `SimpleSpaceMobBase` / `SimpleMobBase`

Respiral adds its own bases:
- `MedievalFloraMobBase` — animals (Flora), use the `MedievalBiological` damageContainer
- `MedievalMonsterMobBase` — monsters, hostile to all factions

## NPC Factions

File: `Resources/Prototypes/_Respiral/NPC/factions.yml`

| Faction | Behavior |
|---|---|
| `TownPeople` | Townsfolk |
| `MedievalHerbivoreFlora` | Herbivores |
| `MedievalWolf` | Wolves |
| `MedievalCat` | Cats |
| `MedievalBear` | Bears |
| `MedievalFox` | Foxes |
| `MedievalHog` | Boars |
| `MedievalMonster` | Monsters — hostile to all other factions |

All factions are mutually hostile with `MedievalMonster`.

## Monsters

Files:
- Base: `Resources/Prototypes/_Respiral/Entities/Mobs/Monster/base.yml`
- Prototypes: `Resources/Prototypes/_Respiral/Entities/Mobs/Monster/monsters.yml`
- Sprites: `Resources/Textures/_Respiral/Mobs/Monster/{name}.rsi`

All monsters use the `MedievalMonster` faction and the AI `MedievalHostileFloraCompound` (from `_Respiral/NPC/flora.yml`).

**IMPORTANT:** entities from `_Respiral` must never use HTN compounds from the original build (e.g. `SimpleHostileCompound`). Only Respiral compounds: `MedievalHostileFloraCompound` (hostile), `MedievalHerbivoreFloraCompound` (herbivore).

| ID | Name | HP | Speed | Damage |
|---|---|---|---|---|
| `MobMedievalBoneHound` | Bone hound | 80 | 6 | Brute 8 |
| `MobMedievalCackle` | Cackle | 60 | 5 | Brute 6 |
| `MobMedievalFlem` | Flem | 100 | 3 | Brute 10 |
| `MobMedievalFlyagaric` | Flyagaric | 50 | 3 | Poison 8 |
| `MobMedievalIceSpectre` | Ice spectre | 70 | 5 | Cold 10 |
| `MobMedievalInvisibleWhistler` | Invisible whistler | 40 | 7 | Brute 12 |
| `MobMedievalLumish` | Lumish | 30 | 3 | Poison 5 |
| `MobMedievalLurker` | Lurker | 120 | 5 | Brute 15 |
| `MobMedievalMole` | Mole | 60 | 4 | Brute 8 |
| `MobMedievalMosquito` | Mosquito | 20 | 7 | Piercing 4 |
| `MobMedievalSpineguard` | Spineguard | 150 | 3.5 | Brute 18 |

## DamageStateVisuals

Standard configuration from the base: state `alive` for Alive, state `dead` for Dead.
Exceptions (no `dead` state in the RSI): `ice_spectre`, `mosquito` — use `alive` for both states.
`lumish` — shows state `lumishroom` on death.

## Key mob components

- `MobState` + `MobThresholds` — Alive/Dead states with an HP threshold
- `DamageStateVisuals` — sprite switching by state
- `NpcFactionMember` — faction membership
- `HTN` — AI behavior tree
- `MeleeWeapon` — melee attack
- `Fixtures` — physics (collider, mass)
- `MovementSpeedModifier` — movement speed

## Profession spawners (RandomHumanoidSpawner)

File: `Resources/Prototypes/_Respiral/Roles/spawners.yml`. Model — `Entities/Mobs/Player/humanoid.yml`.

A profession spawner = a pair of prototypes:
1. `type: entity` (inherits `BaseMedievalHumanoidSpawner` → sprite marker `Markers/jobs.rsi state: green`)
   with a `RandomHumanoidSpawner` component (`settings: <id>`). On MapInit it replaces itself with a humanoid.
2. `type: randomHumanoidSettings` (`id` matches the profession id, like upstream ERTLeader) with
   the components `RandomHumanoidAppearance` (RandomizeName=true by default) and
   `Loadout` (`prototypes: [ <Medieval*Gear> ]`).

`Loadout.prototypes` is a `List<ProtoId<StartingGearPrototype>>`, referencing the `startingGear` from
`_Respiral/Roles/Jobs/*` (which define `equipment:` by slot). This is how the spawner dresses the humanoid in
the profession's gear. All 17 roles covered (Nobles/Craftsman/Villager/Commoner).

No `GhostRole` added (no ghost-role locale for medieval professions) — they spawn an inert NPC, not a
ghost role. If ghost roles are needed — add `GhostRole` to the settings + `ghost-role-information-*` strings.
