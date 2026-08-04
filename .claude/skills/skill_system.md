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
| `Skills` | `List<ProtoId<SkillPrototype>>` | Required skills (mandatory) |
| `Points` | `List<Dictionary<ProtoId, float>>` | Points per hit by skill (mandatory) |
| `LearnModifiers` | `List<SkillLearnModifier>` | Point multipliers by the target's whitelist; if the target matches none — no points are awarded |
| `DamageModifiers` | `Dictionary<ProtoId, float>` | Damage multipliers from skill progress |

### SkillLearnModifier (`[DataDefinition]`)
Path: defined in `NeededSkillForMeleeComponent.cs`

| Field | Type | Description |
|---|---|---|
| `Whitelist` | `EntityWhitelist` | Target whitelist (mandatory) |
| `Modifier` | `float` | Point multiplier (mandatory) |

Modifiers stack multiplicatively.

## NeededSkillForMeleeSystem.OnNeededSkillForMeleeHit logic

1. **Damage**: `BaseDamage` is multiplied by `GetSkillProgress` for each skill
2. **Learning**: for each hit entity:
   - Skip if hitting yourself
   - Compute the multiplier from `LearnModifiers` (summing matches); if 0 — skip
   - Award `value * modifier` points for each skill

## YAML example

```yaml
- type: NeededSkillForMelee
  skills:
  - MedievalOneSword
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
| MedievalBasicBlacksmithing | blacksmithing | MedievalAnvil | MedievalSkillBookBasicBlacksmithing |
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

Two components (`Content.Server/_Respiral/Skill/`), both call `SkillSystem.CheckRequiredSkills`
(which ITSELF shows the popup `skill-need-skill-message` and returns false when lacking;
if the user has no `SkillComponent` at all — it's skipped, i.e. NPCs aren't blocked):

| Component | Event (engine) | What to attach to | What it blocks |
|---|---|---|---|
| `NeededSkillForInteract` | `BeforeInteractionActivate` + `InteractHandEvent` | the target (machine) | activation / hand-click on the object |
| `NeededSkillForInteractUsing` | `BeforeRangedInteractEvent` | the **used item** | applying the item TO something |

- The YAML field for both — `neededSkills: [ ... ]`.
- **Examine description (ExaminedEvent, 2026-06):** all four skill components
  (`NeededSkillForInteract`, `NeededSkillForInteractUsing`, `NeededSkillForMelee`, `LearnSkillWhenUsing`)
  subscribe to `ExaminedEvent` and push a string via `args.PushMarkup`. Skill names are collected by the
  shared helper `SkillSystem.GetSkillNames(List<ProtoId<SkillPrototype>>)` (names from `SkillPrototype.Name`,
  comma-separated). Locale `ru-RU/_respiral/skill.ftl`: `skill-exm-needed-skills` ("Using requires
  the following skills: …") for the three "needed" components (field `NeededSkills`),
  `skill-exm-learn-skills` ("Teaches the following skills: …") for the book (field `Skills`).
  There's no en-US version of skill.ftl — the original is ru only (as before).
- **`NeededSkillForInteractUsing` (2026-06):** it used to subscribe to a custom Respiral event
  `BeforeInteractUsingEvent`, which was raised NOWHERE (never existed in any commit) → the gate was
  dead. Switched to the engine `BeforeRangedInteractEvent` (`Content.Shared/Interaction/BeforeInteract.cs`,
  "raised directed on the used object when clicking on another object") — the exact built-in equivalent,
  requiring NO edits to upstream `SharedInteractionSystem`. The custom `Events/BeforeInteractUsing.cs` was removed.
  `Handled=true` on lacking a skill → `RangedInteractDoBefore` returns true → `InteractUsingEvent`
  on the target isn't raised (application blocked). Picking up/carrying the item is NOT blocked.

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
  pen (apply it on paper/anything). **As of 2026-07 it uses the progress-threshold field (see below):
  `neededSkillsProgress: { MedievalLiteracy: 0.8 }`** — writing unlocks at 80% literacy, not full mastery.

### `NeededSkillForInteractUsing` — progress-threshold gate
- Single field `neededSkillsProgress: { SkillId: threshold }` (`required: true`, 2026-07) — requires
  **progress ≥ threshold** per skill, via `SkillSystem.CheckRequiredSkillsProgress`. Full mastery is just
  threshold `1.0`, so the old `neededSkills` list (which required ≥ 1.0 via `CheckRequiredSkills`) was
  **removed as redundant** — the pen was the only user. Failing blocks the interaction (`args.Handled = true`),
  shows popup `skill-need-skill-message`, and is skipped if the user has no `SkillComponent` (NPCs unaffected).
  Examine (`skill-exm-needed-skills`) lists the gated skill names.
- NOTE: the sibling component `NeededSkillForInteract` (machines) still uses `neededSkills: [ ... ]` +
  `CheckRequiredSkills` (full mastery). That method stays — it's also used by `CanLearn`. Only the
  `...InteractUsing` variant switched to thresholds.

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
- **Other players' skills** — via the `SkillComponent` verb (`SkillShareSystem`, server): sends a
  share-request EUI to the target (accept → `SkillsListEui`); admin-ghost opens directly. The verb
  early-returns when `User == Target`, so it is NOT the path for own skills (that's the character button).

### Console command: addskill (2026-07)

`Content.Server/_Respiral/Commands/AddSkillCommand.cs`, `[AdminCommand(AdminFlags.Debug)]`.
Usage: `addskill <target|self> <skillId> [progress]`.
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
- **Skill gate:** `- type: NeededSkillForInteractUsing` with `neededSkillsProgress: { MedievalEngineering: 1 }`.
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
