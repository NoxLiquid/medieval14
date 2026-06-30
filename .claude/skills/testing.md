# Testing & CI

## Integration tests (Content.IntegrationTests)

- The test pool (`PoolManager`) **always loads all content** (the server+client pair mount all
  prototypes from `Resources/Prototypes`). Loading can't be limited to a subfolder: Respiral prototypes
  inherit (`parent:`) from upstream and reference it. `PoolSettings` only offers `NoLoadContent`
  (no content at all), there's no intermediate "only this folder".
- After loading, a prototype **doesn't keep its source folder/file**. `EnumeratePrototypes<EntityPrototype>()`
  is a flat list of IDs. You can't filter sweep tests "by resource folder" at runtime.
- To determine Respiral membership at runtime, the source of truth is the **folder**, not the ID prefix:
  via `IResourceManager.ContentFindFiles(new ResPath("/Prototypes/_Respiral/"))` + parsing YAML
  we collect the set of `id:` for `type: entity`. ID prefixes are unreliable (see below).

### `--filter` (VSTest/NUnit)
- Selects **test methods by name** (`FullyQualifiedName~...`), it CANNOT reach inside
  a loop method (`EntityTest` iterates all prototypes — it can only be included/excluded as a whole).
- There are ~37 sweep tests in the repo iterating all content (EntityTest, PostMapInitTest, StationPowerTests,
  PrototypeSaveTest, CargoTest, LatheTest, etc.) — excluding them by name is fragile.

### CI strategy (build-test-debug.yml)
**Two separate workflow files** (don't touch `build-test-debug.yml` — it's the upstream gate):
1. `build-test-debug.yml` — **original**, unchanged: full build + all upstream tests.
2. `respiral-test.yml` — **Respiral only**: build → `validate_rsis.py Resources/Textures/_Respiral/`
   → integration tests `--filter "FullyQualifiedName~Tests._Respiral"`.

`continue-on-error`/non-blocking upstream in one file is the outdated variant; now it's split by files.
Assign the required status in branch protection to `respiral-test.yml`, the upstream one — optional.

### Respiral tests (`Content.IntegrationTests/Tests/_Respiral/`, ns `...Tests._Respiral`)
- `RespiralEntityTest.SpawnAndDeleteAllRespiralEntities` — spawn/delete all entities from the folder
  `/Prototypes/_Respiral/` (the ID set from the folder via `IResourceManager`, not from a prefix).
- `RespiralMapTest.RespiralMapsLoadableTest` — loads each map from `/Maps/_Respiral/`
  (`GameDataScrounger.FilesInDirectoryInVfs` + `MapLoaderSystem.TryLoadGeneric`, `InitializeMaps=true`).
- Textures/RSIs are validated NOT by an integration test, but by the `validate_rsis.py` step in the workflow
  (`.rsi/meta.json` format, PNG sizes). Scope — `Resources/Textures/_Respiral/`.
- Map API: `MapLoaderSystem` in `Robust.Shared.EntitySerialization.Systems`, `MapLoadOptions`/
  `DeserializationOptions` in `Robust.Shared.EntitySerialization`. `GameMapPrototype` maps are loaded
  via `GameTicker.LoadGameMap`; arbitrary map files — via `TryLoadMap`/`TryLoadGeneric`.

## Respiral ID prefixes
- The main prefix — `Medieval` (PascalCase compound: `MedievalRoofWooden`, `MedievalWoodenSign`).
- BUT many entities lack this prefix: `Bush*`, `Organ*`, `Mob*`, `Food*`, `Stone*`, `Gallows*`, etc.
  → identifying Respiral content by ID prefix is **not possible**, only by folder.
- Fixed typos (2026-06): `MedivalWoodenSign*`→`Medieval...`, `MedievaHammerAmmoRecipe`→`Medieval...`,
  `Medievalvorosnik`→`MedievalVorosnik` (PascalCase; `воросник` — a plant, the transliteration is correct).
- Namespacing "bare" ids (2026-06): all Respiral-specific prototypes without the prefix were brought to `Medieval<Name>`
  (PascalCase). Affected: 12 `flavor` (`mineral`→`MedievalMineral` etc.), `parallax` `Water/Stone/Black`,
  `npcFaction` `TownPeople`, `soundCollection` `LearnBook`, `entity` `Gallows`. The ftl keys `flavor-base-*`
  were NOT changed (id ≠ loc-key, key references are valid).
- IMPORTANT when renaming short ids (`dry`, `wheat`, `Stone`): change references only in their context
  (`flavors:` blocks, `parallax:` field), not via a blind substring replace — otherwise it hits other words.
- Do NOT touch the ZLevels/CE namespace (`ZLevelEye`, `ZBlur`, `ActionZLevel*`) — it's an MIT port with its own `CE` convention.

## YAMLLinter (`Content.YAMLLinter`, `dotnet run -c Tools`)
- Run: `dotnet run --project Content.YAMLLinter -c Tools`. It loads **both server and client** (two phases:
  `ValidateClient`/`ValidateServer`), so it sees all components of both sides in one factory.
- Parsing/inheritance errors **crash the whole run** (Unhandled exception) before the report — fix
  them one at a time, re-running. Typical crashes (all surfaced 2026-06 after the "base magic" commit):
  1. `KeyNotFoundException: ... 'MedievalMobHuman'` — actually broken YAML in the parent file:
     `components` without a colon (`Flora/Species/human.yml`, block `MedievalBaseSpeciesMobOrganic`) →
     the whole file fails to parse → children can't find the parent.
  2. `ArgumentException: same key ... ComponentRegistration(Action...)` — **two `- type: Action` blocks**
     in one prototype (`Magic/spells.yml`, `ActionSpellFireball`). Merge into one block.
  3. `UnknownComponentException: Unknown name: mana` — case: the component `ManaComponent` →
     name `Mana` (PascalCase), not `mana`. Component names in YAML are case-sensitive.
  4. `KeyNotFoundException: ... 'ActionSpellFireball'` (2026-06) — the base fireball spell was renamed to
     `MedievalActionSpellFireball`, but `MedievalActionSpellWaterball` still had `parent: ActionSpellFireball`
     (missing the `Medieval` prefix). Fixed to `parent: MedievalActionSpellFireball`.
- After the crashes come essentially non-blocking but linter-failing sprite errors on CLIENT
  (`MultipleAssertException`): missing states in an RSI or deleted RSIs. E.g.
  `fire_blade/spectral_blade` looked for `blade/handle` (see combat.md), and `test.yml`
  (`MedievalClothingUniformTest`) references the deleted `_Respiral/Clothing/Test/color.rsi`.
- **As of 2026-06 the linter is CLEAN (`No errors found`).** History of what was fixed to get there:
- The 2 `Magic/spells.yml` errors (`ProjMagicBall` not found; `element` field not on `SpellComponent`) were
  FIXED by aligning YAML to the actual code (the element abstraction is unbuilt, see [[spell]]): point each
  spell's `WorldTargetAction.event.prototype` at the real per-element projectile
  (`MedievalProjFireBall` / `MedievalProjWaterBall`) and drop the nonexistent `element:` field.
- The former 51 `Recipe/packs.yml` errors
  (lathe packs → nonexistent `MedievalClothing*Recipe`) were FIXED 2026-06: on branch `path` the clothing
  (entities + their lathe recipes) was deleted, but a `master` merge resurrected the dangling pack
  references in `packs.yml`. Fix = delete the 51 `- MedievalClothing*Recipe` lines whose entity+recipe no
  longer exist (keep the ones still defined, e.g. belts/helmets/armored boots). To recompute the dangling
  set: for each `- *Recipe` line in `packs.yml`, grep `id: <that>` across `Resources/Prototypes` — no hit = remove.
- **`RespiralMapsLoadableTest` "Missing prototype for map" (2026-06)** — same root cause as the packs:
  deleting clothing on `path` left dangling entity instances in `Maps/_Respiral/*.yml`. A map fails to load if
  ANY referenced prototype is missing. Two were dangling: `MedievalClothingOuterBarrelBib` (2 stray ground
  items → removed the entity blocks) and `MedievalClothingUniformTest` (62 entities **misused as room labels**,
  `MetaData.name` = «Казарма стражей» etc.). Fix for the labels: created a real always-visible marker
  `MedievalMappingMarker` (`_Respiral/Entities/Markers/mapping_marker.yml`, no `Marker` component so it's not
  editor-hidden, no physics, `Markers/cross.rsi`) and re-pointed all 62 `- proto:` blocks to it (names kept).
  To find dangling map protos: extract `proto:` ids from the maps, grep `id: <that>` across `Resources/Prototypes` — no hit = fix.
- During the in-progress **Flora refactor** (FloraComponent.cs deleted, moved to `Flora/Components/`):
  `bushes.yml(75) Failed to parse enum enum.FloraVisuals.State` — the `FloraVisuals` enum lived in the
  deleted component and is now only a commented-out reference (`FloraSystem.cs:148`), while the
  `GenericVisualizer` YAML still uses `enum.FloraVisuals.State`. Re-add the enum (or drop the visualizer).

## Notes
- `TestStationStartingPowerWindow` is marked `[Explicit]` → always `Skipped` in a normal run. If in the
  log after it there's `Error: The operation was canceled` — it's not it that failed, the process was killed (OOM/timeout);
  it's just the last line before stdout was cut off.
- Do YAML validation with the YAML linter, not `dotnet build` (see project memory).
