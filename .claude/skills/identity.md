# Identity / Entity recognition

> **TERMINOLOGY:** the user-facing name of the carrier is **"passport"** (masculine), not "charter".
> It's a booklet: folded (cover `passport_close` + icon `passport_icon` visible) / unfolded
> (`passport_open` + text `passport_open_words`); the stamp is visible only when unfolded. The verbs for
> the "present to all" mode are renamed to fit the booklet: **"Open passport" / "Close passport"**
> (`identity.ftl`). In code/entity names it's still `MedievalPassport`/`MedievalCredential`.
> In the design text below, "charter" = "passport" (a historical design term).

> **STATUS: Phase 1 IMPLEMENTED** (core + "present to all" + creating a passport via stamping).
> Shared/Server/Client builds are clean, YAML linter 0 errors. Phase 2 (per-reader, worn
> heraldry via relay, seal revocation) — NOT implemented, remains the design below.
>
> **Implemented files:**
> - `Content.Shared/_Respiral/Identity/`: `MedievalCredentialComponent`, `CredentialDescription`,
>   `PresentingCredentialComponent`, `GetMedievalCredentialEvent`, `HouseSealPrototype`,
>   `MedievalHouseSealComponent`, `MedievalIdentitySystem`.
> - Hook in `IdentitySystem.GetIdentityName` (line ~245, `// Respiral`) + `using` at the top.
> - Prototypes: `MedievalPassport` (paper.yml), `MedievalHouseSealCrown` (paper.yml),
>   `HouseSealCrown` (house_seals.yml). Locale: `ru-RU/_Respiral/identity.ftl`.
> - **RSI:** medieval paper uses Respiral's OWN copy
>   `Textures/_Respiral/Objects/Items/Bureaucracy/paper.rsi` (a duplicate of upstream `bureaucracy.rsi`,
>   CC-BY-SA-3.0) + 4 own passport states: `passport_close` (folded cover),
>   `passport_icon` (seal icon over the cover), `passport_open` (unfolded),
>   `passport_open_words` (unfolded text). Tint removed — the sprites are hand-drawn.
> - **"Unfolded/folded" visual** (the `PaperVisualizer` pattern): the key `MedievalCredentialVisuals.Presented`
>   (Shared) is set in `SetPresenting`/`OnDropped` via `SharedAppearanceSystem`; the client-side
>   `MedievalCredentialVisualizerSystem : VisualizerSystem<MedievalCredentialVisualizerComponent>`
>   controls ALL passport layers (PaperVisualizer is NOT attached to the passport, to avoid conflict):
>   - base (`MedievalCredentialVisualLayers.Base`) — `ClosedState` (`passport_close`) ↔ `OpenState` (`passport_open`);
>   - icon (`MedievalCredentialVisualLayers.Icon`, `passport_icon`) — visible only when folded;
>   - text (`PaperVisualLayers.Writing`, `passport_open_words`) — visible only when unfolded AND filled
>     (reads `PaperVisuals.Status`);
>   - stamp (`PaperVisualLayers.Stamp`) — visible only when unfolded AND stamped (reads `PaperVisuals.Stamp`).
>   The marker component is in Shared (like `PaperVisualizerComponent`) so the server doesn't choke on an unknown component.
> - **In-game reachability:** added `IdentityBlocker: FULL` to `MedievalClothingMaskExecutioner`
>   and `MedievalClothingMaskPlagueDoctor` (mask.yml) — WITHOUT them there's nothing to hide the face with and the whole
>   mechanism is dead (PresumedName only kicks in with a hidden face).
>   **(2026-08) The face-covering "closed" hoods also carry `IdentityBlocker: FULL`** (head.yml):
>   `MedievalClothingHeadHoodQuiltedClosed` (+ `Red/Brown/Grey` colour variants) and
>   `MedievalClothingHeadHoodChainmailClosed`. The OPEN hoods (`…HoodQuilted`, `…HoodCloth`, `…HoodChainmail`,
>   `…HeadHood`) do NOT block identity (face visible). So blockers in _Respiral = 2 masks + 5 closed hoods.
> - **Tabard affiliation (2026-08):** worn armor with an attached house tabard also sets `PresumedJob`
>   (affiliation, like livery) via `MedievalTabardIdentitySystem` — a SECOND source besides
>   `MedievalCredentialComponent.Affiliation`. It subscribes `ModifyPresumedIdentityEvent` on
>   **`InventoryComponent`** (≠ this system's `IdentityComponent` handler, so no duplicate subscription) and
>   reads the tabard's cached affiliation. See [[tabard]].
> - **Guidebook:** root tab `Medieval` + child tab `MedievalIdentity`
>   (`Prototypes/_Respiral/Guidebook/guidebook.yml`, XML in `ServerInfo/Guidebook/_Respiral/`,
>   names in `Locale/{ru-RU,en-US}/_Respiral/guidebook.ftl`). The guide describes phase 1 + a section
>   "Notes on names" (the visible name = the shown name and may be false; name history; the true name).
> - ⏳ Not done in phase 1: integration test (build-order step 6); manual in-game check.

## The base (upstream) system

`Content.Shared/IdentityManagement/` — the standard SS14 system (NOT Respiral).

- `IdentitySystem.cs` — assembles the name, queues updates on equip/unequip.
- `Identity.cs` — static API `Identity.Name(uid, ent, viewer)`; the model is **global**: the name is the same for all viewers (per-viewer only works via the ghost hack, `CanSeeThroughIdentity`).
- `IdentityComponent` — a slot-container `identity` with a hidden entity whose name others see.
- `IdentityBlockerComponent` — on a mask/helmet/glasses; `Coverage` (MOUTH/EYES/FULL); at FULL cancels `SeeIdentityAttemptEvent`.

### Three name slots (`IdentityRepresentation.ToStringKnown`, `IdentityComponent.cs:56`)
```
face open    → nameOverride ?? TrueName
face hidden  → PresumedName ?? ToStringUnknown()   // "AgeString [PresumedJob] gender"
```
`PresumedName`/`PresumedJob` are currently taken ONLY from `IdCardComponent` (FullName/JobTitle)
inside `GetIdentityRepresentation` (`IdentitySystem.cs:272-283`). `IdCardComponent.FullName` —
just a string, NOT bound to an entity (a bearer document).

## Decisions made (the medieval model)

1. **`PresumedName` only works with a hidden face** (helmet/hood/mask). With an open
   face — the real name (keep the engine default). This is the historical reason for heraldry:
   a coat of arms identifies someone whose face is hidden by a helmet.
2. **Model B — binding by description, hard block.** The document/coat of arms carries a snapshot
   of the owner's description {Gender, AgeBracket, Species}. On presentation, compare against the
   presenter's snapshot; NOT a match → the name isn't issued at all (falls through to "a person in a hood").
   - Limitation: the description is coarse (no "marks/scars"), a stolen one works on a lookalike.
   - Hard binding to `EntityUid` REJECTED: anachronism + clashes with the death/rebirth theme.
3. **Single mechanism.** Coat of arms, seal, and passport are different carriers of one
   "name + description + seal", one Model B code branch.
4. **The seal — a specific house** (`ProtoId<HouseSeal>`): jurisdictions, revoking charters,
   "this seal is forged". Two independent passport checks:
   - description (Model B) → charter ↔ presenter (against theft);
   - seal (heraldry) → charter ↔ issuing house (against forgery).
5. **Estate meaning.** Nobility wear a personal coat of arms → passively "Sir Gawain" (coat = charter).
   A commoner has no coat of arms → anonymous under a hood → presents a sealed passport
   (the seal = the coat of arms of their lord). Heraldry = the privilege of not carrying papers.

### Livery vs personal coat of arms (maps to FullName vs JobTitle)
| Carrier | Gives | Slot | Model B |
|---|---|---|---|
| Livery/common house coat | affiliation "a person of House Raven" | `PresumedJob` | no (like a uniform) |
| Personal coat/seal of a knight | name "Sir Gawain" | `PresumedName` | yes |

## Proposed component
```
MedievalCredentialComponent
├─ Name          → PresumedName
├─ Affiliation   → PresumedJob (unbound)
├─ OwnerDescription {Gender, AgeBracket, Species}  → Model B
├─ Seal: ProtoId<HouseSeal>   → authenticity/jurisdiction
└─ Passive: bool  → a coat of arms powers passively; a passport — by the "present" action
```
Hook: into the assembly of `PresumedName`/`PresumedJob` (currently hardcoded `_idCard.TryFindIdCard`).
Better via a new extension event, so as not to break the upstream method.
Edits in `IdentitySystem` (outside `_Respiral`) → `// Respiral - ...` comments in Russian.

## Creating a passport (Model 2 chosen)
Write the name via the Paper UI + certify with an **official seal** (the `Stamp`/
`PaperVisualizerSystem` system already exists). Stamping with the house seal = a trigger that copies the name +
a description snapshot into the readable field. Forgery = stealing the seal AND resembling the owner.

> **UPDATED — target selection + DoAfter (IMPLEMENTED).** The old snapshot source `GetHolder`
> (whoever physically holds the passport during stamping) is REMOVED: you can't click an item held in
> *another* person's hands, so "owner holds, official stamps" was unreachable. New flow, all on the
> passport's single `AfterInteractEvent` → `MedievalCredentialDoAfterEvent` (both branches go through a
> DoAfter, `MedievalCredentialComponent.Delay`, default 2s, `BreakOnMove`/`BreakOnDamage`/`NeedHand`):
> - **Passport not sealed (`Seal == null`)** → apply passport on a person (needs `HumanoidProfileComponent`)
>   → records that person into new field `MedievalCredentialComponent.Target` (`EntityUid?`, `AutoNetworkedField` —
>   the source generator auto-converts `EntityUid`↔`NetEntity`, so NOT a raw `NetEntity` field; matches
>   `PresentingCredentialComponent.Passport`). Ranged, the future owner need not hold the passport.
> - **Passport sealed** → same interaction becomes "present to one" (unchanged logic, now behind the DoAfter).
> `OnInteractUsing` (seal on passport) now snapshots `OwnerDescription` from `Target` (not the holder) and
> ONLY sets `Seal` when `Target` is a valid humanoid — otherwise pops `medieval-credential-certify-no-target`
> and leaves the passport unsealed (avoids the soft-lock: sealed-but-descriptionless + can no longer pick a target).
> New locale: `medieval-credential-target-selected`, `medieval-credential-certify-no-target` (`identity.ftl`).
> Guidebook `Identity.xml` "Как создать паспорт" now has 3 steps (write → pick owner → seal).
> Removed the now-unused `_container` dependency + `GetHolder` helper. `Content.Shared` build clean.
>
> **UPDATED — auto-fill of the description on certification (IMPLEMENTED).** When the seal snapshots
> `OwnerDescription` in `OnInteractUsing`, it now ALSO writes those traits (sex, species, age bracket)
> into the paper's readable `Content` via `PaperSystem.SetContent`. The written name is read into
> `cred.Name` BEFORE appending (order matters, otherwise the appended block would become the name).
> Content becomes `"<name>\n\n<description>"` (or just the description if the paper was blank). Deps:
> `PaperSystem _paper`; private `GetGenderRepresentation(Gender)` mirrors the upstream
> `identity-gender-*` keys (`using Robust.Shared.Enums`). Locale `medieval-credential-description`
> ("Приметы: { $gender }, { $species }, { $age }.") in `ru-RU/_Respiral/identity.ftl`. Species via
> `HumanoidProfileSystem.GetSpeciesRepresentation(...).ToLower()`, age via `GetAgeRepresentation`
> (same call reused for the snapshot). `Content.Shared` build clean.

## Presenting the passport — TWO modes (both chosen)
One act of "unfolding the charter", two visibility scopes, different lifetime of the knowledge:

| | "Present to all" | "Present to one" |
|---|---|---|
| Audience | everyone in PVS | one chosen viewer |
| Lifetime | transient (while unfolded) | durable (the viewer remembers) |
| Slot | global `PresumedName` (hook `GetMedievalCredentialEvent`) | viewer's `KnownIdentities` (`CanSeeThroughIdentity`) |
| Cost | cheap, no engine edits | per-viewer layer |
| Model B | against the presenter | against the presenter; no match → the viewer doesn't learn |

**Phase 1 — common core + "present to all":**
`MedievalCredentialComponent` + Model B + Seal validation + hook in `GetIdentityName`.
"Present to all" = transient "unfolded" state → the credential powers `PresumedName`.
Verb toggle. The engine is untouched (except the 3-4 line hook insertion).

**Phase 2 — "present to one" (an add-on, not a rewrite of phase 1):**
`KnownIdentitiesComponent` on the viewer + extension of `CanSeeThroughIdentity` (`Identity.cs:73`,
currently only Ghost) + `RaiseNetworkEvent(ev, session)` to the reader's session + threading
`viewer` through call sites (examine/context/overhead — the main hidden volume, often `viewer == null`).
This is also the foundation for path C (recognition by personal acquaintance) — the passport is just the first consumer.

## Implementation plan — Phase 1
Goal: core (credential + Model B + seal) + "present to all" (globally, transiently)
+ passport creation (write + certify with a seal). Per-reader — NOT here.

### Code in `_Respiral` (`Content.Shared/_Respiral/Identity/`, ns `Content.Shared._Respiral.Identity`)
| File | Contents |
|---|---|
| `MedievalCredentialComponent.cs` | `Name`, `Affiliation` (LocId→PresumedJob), `OwnerDescription`, `Seal: ProtoId<HouseSealPrototype>?` |
| `CredentialDescription.cs` | snapshot struct: `Gender`, `AgeBracket`, `Species` — what Model B compares |
| `PresentingCredentialComponent.cs` | transient on the carrier: `EntityUid Passport`; sets the verb, removed on hand-swap/toggle |
| `GetMedievalCredentialEvent.cs` | `[ByRefEvent] record struct(IdentityRepresentation)`, relayed (groundwork for worn heraldry) |
| `MedievalHouseSealComponent.cs` | on the seal: `ProtoId<HouseSealPrototype> House` |
| `HouseSealPrototype.cs` | house prototype (id, name, color, RSI state) — "a specific coat of arms" + jurisdictions |
| `MedievalIdentitySystem.cs` | all the logic |

`MedievalIdentitySystem`:
1. `GetMedievalCredentialEvent` handler → looks for `PresentingCredentialComponent` (passport in hand)
   → Model B (compare `OwnerDescription` with the current Gender/Age-bracket/Species from `HumanoidProfileComponent`).
   Match + valid seal → `Representation.PresumedName = cred.Name` (Affiliation→PresumedJob).
   No match → do NOT touch (hard block).
2. Verb "Present the charter" (on the passport in hand) → toggle `PresentingCredentialComponent`
   → `QueueIdentityUpdate(carrier)` (upstream public method).
3. Creation (Model 2): `MedievalHouseSeal` `AfterInteract` on an entity with `Paper` → snapshot the CURRENT
   holder's description + read the written name into `cred.Name` + `cred.Seal = House` + a cosmetic stamp.

### Insertion into upstream (outside `_Respiral`, needs `// Respiral -` comments)
`IdentitySystem.cs`, method `GetIdentityName` (line 241), between `SeeIdentityAttemptEvent` and
`ToStringKnown`: raise `GetMedievalCredentialEvent(representation)` + `using …_Respiral.Identity;`.
Total footprint — 3-4 lines.

### Prototypes (`Resources/Prototypes/_Respiral/Entities/Objects/Bureaucracy/`)
- `MedievalPassport` in `paper.yml`: `MedievalBasePaper` + `Paper` + UI entries + `MedievalCredential`,
  RSI state `passport`. Closes the "lying" issue from bureaucracy.md.
- House seals: `RubberStampBase` + `Stamp` + `MedievalHouseSeal`.
- `house_seals.yml` — list of `HouseSealPrototype`.
- Locale: `ru-RU/_Respiral/identity.ftl` (Affiliation, verbs, "the charter describes a different person").

### Build order
1. Component + `MedievalPassport` (no logic) → YAML linter (not `dotnet build`).
2. Hook + insertion + PresumedName handler → `dotnet build`.
3. Verb + transient → manual check: hood→anonymous→presented→name to all→removed→anonymous.
4. Model B: someone else's passport on a lookalike/non-lookalike → block on mismatch.
5. Creation: wrote → certified with a seal → description snapshot + Seal.
6. Integration test (`Content.IntegrationTests`, ID prefix per testing.md): `Identity.Name` before/after/on mismatch.

### Gotchas (found during recon)
- **Hands ≠ relay:** relay events travel through equipment slots, a passport in hand doesn't reach them
  → hence `PresentingCredentialComponent`, not a subscription on worn gear (relay is groundwork for the coat of arms in phase 2).
- **Literacy:** writing the name goes through `_Respiral/Illiteracy` (the `Paper` behavior) —
  an illiterate person can't write/read; check that this doesn't break creation.
- **The description snapshot** is taken from the holder at the moment of stamping → the future owner must hold the passport during certification.

## Implementation plan — Phase 2 (estimate)
Goal: an open face is anonymous to strangers; the real name is seen only by those who know.
This isn't "adding", it's **flipping the default**: the public name becomes anonymous even with an
open face, while acquaintances "see through" it via `Identity.Name(uid, ent, viewer)` /
`CanSeeThroughIdentity` (`Identity.cs:73`).

**Fail-closed is mandatory:** the global name = anonymous; forgot to thread `viewer` → an acquaintance sees
anonymous (annoying but NOT a leak). The reverse default would leak at every uncovered spot.

The seam logic: `sees_name = knows_target && face_open` (+ Ghost). Consequence: a mask hides even
from acquaintances → `SeeIdentityAttemptEvent` (face coverage) stays in play.

### Blocks and estimate
- **A. Flip the default** — `[S]` of code, `[L]` of radius. Global identity is always anonymous;
  breaks upstream expectations (crew manifest, records, consoles) — needs a review.
- **B. "Who knows whom" data** — `[M]`. `KnownFacesComponent` on the viewer (set of `NetEntity`),
  owner-only / targeted event to a session. You always know yourself.
- **C. Extend `CanSeeThroughIdentity`** — `[S]`. `+ knows_target && face_open`.
- **D. Cover the name-display points** — `[L]`, the MAIN effort. Measured: **69** `Identity.Name` calls,
  of which **~48 without `viewer`**, **21** with viewer.
  - Server text (examine, logs) → thread `viewer`. Voluminous but mechanical.
  - **Chat — a separate pain:** the name is baked into a string broadcast to everyone at once (`ChatSystem.PrivateAPI`);
    a per-recipient name requires assembling the message per recipient.
  - **Client widgets** (overhead, context menu, outline) read the identity entity's name
    directly, bypassing `Identity.Name(viewer)` → needs a client-side override based on local `KnownFaces`.
- **E. Source of recognition** — `[M]`. "Present to one": a verb on the target + DoAfter + description check
  (phase 1 logic) → add the presenter to the viewer's `KnownFaces` (event to their session).
- **F. Tuning** — `[M]`, design. Starting acquaintance (otherwise at round start you know no one —
  probably "know" your house/family/squad), forgetting/lifetime, recognition-by-wanted (CriminalRecords).
- **G. Real privacy** — `[L]`, DEFER to phase 3. Currently the name is still replicated to all
  clients (anonymous only at the UI level). Honest hiding = don't send the name to PVS for those who don't know.

### Bottom line and the start
The skeleton (A+B+C+E) — "medium"; **D dominates** and pulls it into "large" (48 sites + chat + widgets).
**De-risking spike `[S]` first:** flip the default (A) + extend the seam (C) + cover ONLY
examine → empirically see where the name leaks. This turns the "L" of point D into a countable list.

### De-risking spike results (IMPLEMENTED, marked `// Respiral SPIKE Phase2`)
Files: `KnownFacesComponent` (new); edits to `IdentitySystem.GetIdentityName` (flip to `false`),
`Identity.CanSeeThroughIdentity` (+ self + KnownFaces), `MedievalIdentitySystem.OnPresentToOne`
(click a passport on a person → the target "recognizes" the presenter), popups in `identity.ftl`. Build clean.
Rollback: return `!ev.Cancelled` in `GetIdentityName`.

> **UPDATED:** "show to one" remembers the **name FROM THE PASSPORT**, not the real one, and keeps
> a **personal note about names** for each person.
> `KnownFacesComponent.Known` = `Dictionary<NetEntity, KnownFace>`; `KnownFace` (DataDefinition,
> NetSerializable) = `List<string> ShownNames` (history of shown names in order; the last — the current)
> + `string? TrueName` (the true name, if known; revealed by magic — nothing sets it yet).
> `Identity.Name` for an acquainted viewer: `TrueName ?? ShownNames[^1]` (the true name takes precedence over the shown one);
> self/ghost still see `meta.EntityName`. `CanSeeThroughIdentity` — `ContainsKey`
> (used in `Identity.Entity` for loc — grammar from the real entity, correct: the description
> matches by sex/species). Showing a new passport appends the name to `ShownNames` (a consecutive duplicate isn't written).
> `OnPresentToOne` requires `cred.Name != null`.
> **Examine verb "Names"** (`MedievalIdentitySystem.OnIdentityExamineVerb`, modeled on armor via
> `ExamineSystemShared.AddDetailedExamineVerb`, icon `human-head-eyes`): appears when examining a
> person if the viewer has a note about them; reveals "Current name / Formerly called / True name".
> Locale `medieval-identity-notes-*` in `ru-RU/_Respiral/identity.ftl`.
> **Consequence:** another player's real name CANNOT be learned by ordinary means — only self/ghost/admin.
> A passport can be forged (false name with a matching coarse description).
> **DECIDED and IMPLEMENTED:** the channel to "learn the true name" = **MAGIC** (not a registry/wanted/guard).
> The true name is revealed by the spell `ActionSpellRevealTrueName` (the "ritual on target" form, see
> [[spell]]): `EntityTargetAction` on a person → DoAfter → `RevealTrueNameSpellSystem` writes
> `face.TrueName = Name(target)` (=`meta.EntityName`) into the CASTER's `KnownFacesComponent`. It bypasses both
> anonymity and a false passport (`Identity.Name` returns `TrueName` over the shown one). Other channels
> (house registry, CriminalRecords, open face) REJECTED in favor of the magic one.
> Files: `Content.Server/_Respiral/Magic/Spell/RevealTrueNameSpellSystem.cs`,
> `Content.Shared/_Respiral/Magic/Spell/RevealTrueNameSpell{Event,DoAfterEvent}.cs`,
> prototype in `Prototypes/_Respiral/Magic/spells.yml`, locale `magic/spells.ftl` (`spell-reveal-truename-*`).
> The spell is NOT YET given to anyone (like the balls) — an action must be given to a player/item.

**"Where the name leaks" map (the spike's main product):**
- ✅ **Per-viewer works out of the box** (an acquaintance sees the name, a stranger — anonymous): **examine**
  (`ExamineSystem.cs:241`), **context menu** — the entry (`EntityMenuElement.cs:96`),
  interaction popups with user/target (Cuffs ×15, Internals, Implanter). This proves the architecture.
- ⚠️ **Anonymous to all (fail-closed, an acquaintance does NOT see through)** — needs `viewer` threading:
  - **Chat — the largest visible gap:** `ChatSystem.PrivateAPI` + ZLevels `CEZLevelsSpeakingSystem`
    call `Identity.Name(source)` without viewer; the speech bubble takes the name from the message. Per-recipient =
    assembling the string per recipient (the confirmed "pain").
  - Consoles/records: CriminalRecords (+console), Cloning, Admin, HealthAnalyzer, Strip, Pinpointer.
  - Context menu — grouping (`EntityMenuUIController`) counts by anonymous (sort/group), the entry itself is covered.
  - Other popups without viewer: Lock, Buckle, Borg, SmartFridge, Hands…
- 🚫 **Not about identity** (~15 of 48): vending/gas/item-status/storage — these are item names, not identity → exclude from list D.

**Conclusions:** (1) the seam works — examine/context are correct without edits, architecture confirmed;
(2) fail-closed confirmed — everywhere else degrades to anonymous, no name leak; (3) the real point D
= a countable list (~33 "identity" sites), dominated by **chat** (which is also the hardest — per-recipient).
**Spike simplifications:** `CanSeeThroughIdentity` does NOT check face concealment (an acquaintance under a mask is still
recognized for now); `KnownFaces` without forgetting and starting acquaintance.
> **NOTE PRIVACY (done):** `KnownFacesComponent` is now **owner-only** — the
> `ComponentGetStateAttemptEvent` subscription in `MedievalIdentitySystem.OnKnownFacesGetStateAttempt`
> cancels sending state to everyone except the owning session (`args.Player?.AttachedEntity != ent.Owner`).
> The server keeps a full copy. This closes the "other people's notes are visible to all clients" leak.
> NB: this is NOT phase 3 — phase 3 is about the leak of the identity name itself (MetaData is replicated to all).

## Review fixes (2026-07)
Code review of `_Respiral/Identity`. Fixed (per user triage — points 2/8/10 intentionally NOT changed):
- **[P1] See-through inconsistency (Identity.cs).** `CanSeeThroughIdentity` returned `true` on a
  mere `KnownFaces.ContainsKey`, so `Identity.Entity(uid, ent, viewer)` returned the REAL entity →
  real `MetaData.EntityName` leaked into any loc that threads `viewer` (currently only 1 site,
  `GuardianSystem.cs:213` — latent). Now see-through requires knowing the **true name**
  (self / ghost / `face.TrueName != null`). A passport-only acquaintance gets the anonymous identity
  entity for loc grammar (fail-closed; grammar still matches, Model B guarantees sex/species). The
  shown/passport name still reaches them via `Identity.Name`.
- **[P1] Reprint / empty seal (`OnInteractUsing`, state model Draft→Sealed).** Certification is now
  ONE-SHOT: `Seal != null` → reject (`medieval-credential-already-sealed`). Blank paper → reject
  (`medieval-credential-empty`). On seal: read name from paper (`SanitizeName`), snapshot description,
  append traits ONCE, set `paper.EditingDisabled = true` (tamper-proof even vs cyberpen), set `Seal`.
  Removed the old "seal blank passport" softlock and the double-append name-corruption bug.
- **[P1] Presented passport not hand-bound (`OnGetCredential` + events).** `OnGetCredential` now
  requires `_hands.IsHolding(carrier, passport)` (fail-safe every identity assembly). `DroppedEvent`
  → replaced with `GotUnequippedHandEvent` (covers drop / handoff / theft / container). `SetPresenting`
  now folds the sprite of a PREVIOUSLY presented passport when switching to a new one.
- **[P2] Markup injection (`BuildNotesMarkup`).** Player-controlled names are now
  `FormattedMessage.EscapeText`-ed before `AddMarkupOrThrow`; source name is single-line + length-capped
  (`SanitizeName`, `MaxNameLength = 64`). Chat already escaped (`ChatSystem.PrivateAPI:107`).
- **[P2] Literacy bypass (`OnCredentialDoAfter`).** "Present to one" now gates on the RECIPIENT's
  `MedievalLiteracy` ≥ `MedievalCredentialComponent.ReadingLiteracy` (default 0.5) via `GetLiteracy`
  (reads `SkillComponent` directly, mirrors client `PaperWindow.GetLocalLiteracy`; no `SkillComponent`
  → treated as literate). Illiterate reader learns nothing (`medieval-credential-present-one-illiterate`).
- **[P2] Age bracket as enum.** `CredentialDescription.AgeBracket`: `string` → `enum AgeBracket`
  (`Young/MiddleAged/Old`). `GetAgeBracket(species, age)` computes from `SpeciesPrototype.YoungAge/OldAge`;
  `GetAgeBracketRepresentation` localizes only at display. Locale change no longer desyncs passport↔owner.

**Intentionally NOT changed:** [P1] "closed passport still readable" (by design — paper UI opens in
hand; unfolded = present to all); [P2] memory on body not mind (by design — memory should reset on
body swap); [P2/P3] secrets in PVS (`AutoNetworkedField`) — deferred: hiding component fields is moot
while `PaperComponent.Content` is networked; true fix (gate paper content) is phase 3.

## Open questions
- The mechanic of revoking charters / "expiring" seals.
- Where `OwnerDescription` comes from on issuance (a snapshot of the presenter at the moment of stamping?).
- Lifetime of `KnownIdentities`: per round / with forgetting.
- "Present to one": initiated by the carrier (voluntarily) or the reader (a search/"verify the charter")?
