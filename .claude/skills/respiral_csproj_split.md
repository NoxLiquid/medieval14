# Respiral csproj Split (REVERTED — kept the event hooks only)

> **Status: REVERTED 2026-07 by user decision.** The full split (Content.Respiral.Shared/.Server/.Client)
> was built end-to-end (compiled + YAMLLinter green) and then **rolled back**. Rationale (user): the split's
> headline benefit — "don't touch upstream" — is not realized, because the ~52 inline upstream patches
> (color literals, method-body edits, overrides) MUST stay inline regardless, and *those* are where all the
> merge pain is. The additive `_Respiral/` code almost never conflicts on merge whether it lives in a
> separate assembly or a folder. So the split only added build complexity (OutputPath hacks, explicit
> assembly-load wiring in 4 places, the no-reference cycle rule, the XamlIL nested-build race) for a mostly
> symbolic boundary. **Kept:** the Phase-0 event/interface decoupling (see below) — it's good hygiene on its
> own and the user liked it. **Reverted:** all file moves + the 3 new csproj + all wiring.
>
> **What "reverted" concretely means:** all Respiral code is back under `Content.Shared/_Respiral`,
> `Content.Server/_Respiral`, `Content.Client/_Respiral` (single assembly per layer, as before). The upstream
> files no longer name Respiral types — they raise events / use an interface instead (the hooks below), and
> Respiral systems (now in the same assembly again) subscribe. This compiles fine single-assembly because
> events are assembly-agnostic.
>
> **How the revert was done (git, since the whole split was uncommitted — staged renames + working-tree
> adds, HEAD = clean pre-split):** `git checkout HEAD --` on the 9 pure-wiring files (slnx, PoolManager,
> ContentUnitTest, Client/ServerPackaging, the 4 csproj) → back to vanilla; `git mv` each subsystem dir back
> (preserves the handler edits); `rm -rf` the 3 scaffolding dirs + `git add -A`; **delete stale
> `bin/**/Content.Respiral.*.dll`** (else the engine's `Content.*` prefix scan loads them → duplicate type
> registration). A blanket `git reset --hard` was NOT used — it would also wipe the event hooks.
>
> ## The event hooks that WERE KEPT (this is the durable outcome)
>
> These make upstream stop *calling* Respiral systems directly — upstream now raises an event that a Respiral
> handler subscribes to. **The event TYPES live under `_Respiral` namespaces** (user decision 2026-07: keep
> all Respiral code, incl. its events, namespaced under `_Respiral`; single-assembly, so upstream just adds a
> `using Content.*._Respiral.X`). Valuable as loose coupling regardless of assembly layout.
> - **Dirt / chameleon:** event `ChameleonClothingUpdatedEvent(EntityPrototype)` in
>   `Content.Shared/_Respiral/Dirt/ChameleonClothingUpdatedEvent.cs`. `SharedChameleonClothingSystem` raises it
>   (`using Content.Shared._Respiral.Dirt`); `SharedDirtSystem` handles it on `DirtableComponent`.
> - **Identity:** `Content.Shared/_Respiral/Identity/RespiralIdentityEvents.cs` defines
>   `ModifyPresumedIdentityEvent`, `ResolveIdentityNameEvent`, `CanSeeThroughIdentityEvent`. `IdentitySystem`
>   raises the first; the static `Identity` class raises the other two via `ent.EventBus.RaiseLocalEvent`
>   (both upstream files add `using Content.Shared._Respiral.Identity`). `MedievalIdentitySystem` (same
>   namespace) handles all three. Old `GetMedievalCredentialEvent.cs` deleted (replaced by `ModifyPresumedIdentityEvent`).
> - **Pricing:** `Content.Server/_Respiral/Pricing/PricingCompositionEvents.cs` defines
>   `GetPricedByCompositionEvent` (entity) + `GetEstimatedPricedByCompositionEvent(EntityPrototype)` (broadcast).
>   `PricingSystem` raises them (`using Content.Server._Respiral.Pricing`) instead of naming
>   `MedievalPhysicalPricedComponent`; `MedievalPhysicalPricedSystem` answers.
> - **Client dirt visuals:** `Content.Client/_Respiral/Dirt/AddEquipmentDirtLayerEvent.cs` defines
>   `[ByRefEvent] AddEquipmentDirtLayerEvent` (carries params + mutable `Index` since a struct can't hold
>   `ref int`). `ClientClothingSystem` raises it (`using Content.Client._Respiral.Dirt`); `DirtVisualsSystem`
>   subscribes on `DirtableComponent`.
> - **Parallax:** NOT an event — the `IBiomeRegionLayer` interface attempt was **reverted** (2026-07). The
>   type-switch in `SharedBiomeSystem` (`is BiomeRegionLayer` in a hot map-gen loop) doesn't fit the event
>   pattern, so it's back to naming the concrete Respiral `BiomeRegionLayer` directly, exactly as before
>   (`using Content.Shared._Respiral.Parallax.Biomes.Layers`). This is the one remaining upstream→Respiral
>   *type* reference that stayed inline (like `ProjectileSystem`→Dirt, `PaperWindow`→Skills, `Controllers`).
>
> ---
> The design/history below is retained for reference (why the split is *possible*, the load mechanics, the
> blocker analysis) in case a split is ever reconsidered. **It does not reflect the current tree.**
>
> Read together with [[upstream_patches]] (the inline-patch map) and [[engine_submodule]] (how the
> engine loads content assemblies).

## SCOPE DECISION (2026-07): `_CE` stays in upstream assemblies

**`_CE` (CrystallEdge / ZLevels, MIT) is EXCLUDED from the split — leave it where it is.** Only the
pure-Respiral (GPL) `_Respiral` code moves to `Content.Respiral.*`. Consequence: any Phase 0 item that
exists solely to decouple `_CE` from upstream is **out of scope** (the `_CE` ↔ upstream coupling stays
inline, as today). This drops the `CCVars` and `ScalingViewport` partials and the `ThrownItemSystem` /
`ParallaxOverlay` refs from the work list.

## Progress log (live checklist)

**Phase 0 — decouple (mandatory gate). Verified coupling: 13 upstream refs + 4 name-matched partials.**

Boundary partials (4):
- [x] **`MobStateCondition`** — DONE 2026-07. Was a **false positive**: the Respiral
  `Content.Shared.Interaction.MobStateCondition : InteractionCondition` is a *separate* type from the
  upstream `EntityConditions...MobStateCondition` (different namespace + base); `partial` only for the
  DataDefinition source-gen, not cross-assembly. Renamed → `MobStateInteractionCondition` to kill the
  fragile short-name clash (same YAML `!type:` tag resolved to two unrelated types by field context).
  Updated 3 Respiral YAML tags (`_Respiral/Interactions/*.yml`); upstream reagent YAMLs untouched.
  Build + YAMLLinter clean.
- [x] **`WhitelistCondition`** — DONE 2026-07. Same false positive (vs. upstream
  `Content.Server.Connection.Whitelist.WhitelistCondition`). Renamed → `WhitelistInteractionCondition`
  (no YAML usages existed). Build clean.
- [~] `CCVars` (`_CE` ZLevels CVars) — **OUT OF SCOPE** (`_CE` stays). Left as `partial CCVars`.
- [~] `ScalingViewport` (`_CE RenderZLevels`) — **OUT OF SCOPE** (`_CE` stays).

Upstream→Respiral refs:

**Content.Shared → _Respiral (the ONLY true circular blockers — DONE 2026-07):**
- [x] `Identity.cs` → `_Respiral.Identity.Components` (`KnownFacesComponent`) — inverted via upstream
  ref events `ResolveIdentityNameEvent` / `CanSeeThroughIdentityEvent` (raised on viewer via
  `ent.EventBus.RaiseLocalEvent`), handled by `MedievalIdentitySystem` on `KnownFacesComponent`.
- [x] `IdentitySystem.cs` → `_Respiral.Identity` — the Respiral `GetMedievalCredentialEvent` was
  replaced by upstream `ModifyPresumedIdentityEvent` (new file `RespiralIdentityEvents.cs`); old event
  file deleted. `MedievalIdentitySystem.OnGetCredential` now subscribes to the upstream event.
- [x] `SharedChameleonClothingSystem.cs` → `_Respiral.Dirt(.Components)` — inverted via upstream
  `[ByRefEvent] ChameleonClothingUpdatedEvent(EntityPrototype)` (defined at bottom of the upstream
  system file), handled by `SharedDirtSystem` on `DirtableComponent`.
- [x] `SharedBiomeSystem.cs` → `_Respiral.Parallax.Biomes.Layers` — the HARD one. Introduced upstream
  extension interface `IBiomeRegionLayer : IBiomeLayer` (new file, **must be `partial interface`** —
  `[ImplicitDataDefinitionForInheritors]` on `IBiomeLayer` propagates, else `RA0017`). `BiomeRegionLayer`
  now implements it; `SharedBiomeSystem` matches `is IBiomeRegionLayer` / `case IBiomeRegionLayer:`
  instead of the concrete type. No `switch`-polymorphism needed after all.

**Content.Server/Client → _Respiral (NOT circular — those projects reference Content.Respiral.Shared;
consume the kept namespaces, no change needed):** `PricingSystem` (→ Content.Server/_Respiral, internal),
`ProjectileSystem` (→ shared Dirt), `ClientClothingSystem`/`AdminbusTab`/`CraftingUIController`/
`SandboxUIController` (→ Content.Client/_Respiral), `PaperWindow` (→ shared Skills via `Literacy`).

Upstream→`_CE` refs — **OUT OF SCOPE** (`_CE` stays): `ThrownItemSystem.cs`, `ParallaxOverlay.cs`.

### Phase 1 + first move — DONE 2026-07 (`Content.Respiral.Shared` created, `Lock` moved)

`Content.Respiral.Shared` assembly is live and the first decoupled subsystem (**`Lock`**, 4 shared files,
0 inbound `Content.Shared` refs) moved into it. Server/Client/IntegrationTests build clean + YAMLLinter green.

**How it was wired (reusable recipe for the next moves):**
1. `Content.Respiral.Shared/Content.Respiral.Shared.csproj` — modeled on `Content.Shared.csproj`
   (same props, `WarningsAsErrors=RA0032;nullable`, CompNetworkGenerator + EntitySystemSubscriptions
   generators), single `ProjectReference → Content.Shared` (one-way).
2. **`Content.Respiral.Shared/GlobalUsings.cs`** — MUST mirror `Content.Shared/GlobalUsings.cs`
   (EntityUid/DataField/Dependency/etc. are global usings; without it every moved file errors).
3. Referenced from **`Content.Client`, `Content.Server`, `Content.IntegrationTests`** (all consumers of
   `_Respiral` shared types) + added to `SpaceStation14.slnx`. NOT referenced by `Content.Shared` (would
   be circular — that's the Phase 0 gate for coupled subsystems).
4. Moved files via `git mv Content.Shared/_Respiral/Lock → Content.Respiral.Shared/Lock`. **Namespace
   kept** `Content.Shared._Respiral.Lock.*` (minimize churn; namespace ≠ folder is fine).

**Assembly-load gotchas (the real work — beyond just moving files):**
- **The game loads by `Content.` filename-prefix scan** (`GameController.cs:350` / `BaseServer.cs:330`
  → `ModLoader.TryLoadModulesFrom`). So `Content.Respiral.Shared.dll` (copied to `bin/Content.*` via the
  references) auto-loads at runtime — no game change needed.
- **The test/tooling harness loads by an EXPLICIT assembly list, NOT prefix scan.** Any moved subsystem's
  components/prototypes are "Unknown" until the assembly is added to these lists:
  - `Content.IntegrationTests/PoolManager.cs` `Startup()` shared list (covers YAMLLinter + integration tests).
  - `Content.Tests/ContentUnitTest.cs` `GetContentAssemblies()` shared list.
  Added `typeof(Content.Shared._Respiral.RespiralSharedAssemblyMarker).Assembly` (stable marker type in the
  new assembly, so the list never pins a movable type).
- **Packaging:** server `ServerPackaging.GetContentAssemblyNamesToCopy` auto-discovers from
  `Content.Server.deps.json` (auto-included). Client `ClientPackaging.cs` has a **hardcoded** name array —
  added `"Content.Respiral.Shared"`. (`Content.MapRenderer/Benchmarks/Replay` use the real prefix-scan path → no change.)

**Moved so far — ALL 14 shared 0-inbound leaves DONE (2026-07):** `Lock`, `Flora`, `Blacksmithing`,
`Barrel`, `EntityEffects`, `FriendlyFaction`, `Magic`, `TimeWindow`, `CCCVar`, `Temperature`, `Mount`,
`Offer`, `Burning`, `Interaction`. All via step 4 (`git mv`), no code edits (namespaces kept, incl.
upstream-style ones like `Content.Shared.Temperature`/`.Burning`/`.Mountable`/`.Offer`/`.Interaction` —
no FQN collision since the Respiral type names are unique). Server/Client/IntegrationTests build clean +
YAMLLinter green after each batch. Verified 0-inbound by *type name* (not just namespace) — the only
apparent hit (upstream `MobStateCondition`) is a same-short-name different-FQN type, not a real ref.
Gotcha: `git mv` of a whole dir may hit Windows "Permission denied" on a locked subdir — move children
individually (worked for `Interaction/Prototypes`).

### Coupled shared subsystems — Phase 0 + move DONE 2026-07 (`Dirt`, `Identity`, `Parallax`, `Skill`)

All four remaining shared subsystems decoupled (see the Content.Shared→_Respiral checklist above) and
moved to `Content.Respiral.Shared` via `git mv` (namespaces kept `Content.Shared._Respiral.*`).
Content.Shared / Server / Client / IntegrationTests all build clean; upstream `grep` for
`Content.Shared._Respiral` is empty.

- **Move order gotcha:** `Identity` uses `Skill` (`Literacy.GetLevel`) → they had to land in the SAME
  assembly. Both moved together; harmless since all four go to one assembly. Dirt/Identity/Parallax do
  not cross-reference each other; ZLevels does not reference any of the four (verified before moving).
- **`git mv` of the whole `Identity` dir failed** because of the staged-deleted `GetMedievalCredentialEvent.cs`
  (git tried to move the tombstone). Fix: move the dir's files individually via `git ls-files … | while read`,
  then `git rm --cached` the deleted file and `rm -rf` the now-empty dir.
- **No PoolManager / ContentUnitTest change needed** — subsystems moved *within* the already-registered
  `Content.Respiral.Shared` assembly (marker `RespiralSharedAssemblyMarker` already in both lists).

**Remaining in `Content.Shared/_Respiral`:** only `ZLevels` (`_CE`, **OUT OF SCOPE** — stays).

### Server + Client assemblies — DONE 2026-07 (`Content.Respiral.Server` / `Content.Respiral.Client`)

Both created and populated. Moved **all** `Content.Server/_Respiral/*` (17 dirs: Blacksmithing, Burning,
Commands, Construction, Destructible, Flora, Hanging, Light, Lock, Magic, NPC, Offer, Pricing, Skill,
Temperature, Trade, Wear) and **all** `Content.Client/_Respiral/*` (8 dirs: Barrel, Blacksmithing, Burning,
Dirt, Identity, Lock, Offer, Skill) via `git mv`, namespaces kept. Full solution + IntegrationTests build
clean; YAMLLinter green.

**Staying in upstream (NOT moved):**
- `ZLevels` (`_CE`) in both Content.Server and Content.Client — out of scope.
- **`Controllers`** in Content.Client — `MedievalEntity/TileSpawningUIController` (standalone `UIController`s
  upstream resolves by concrete type via `GetUIController<T>()`) + `MedievalConstructionMenuPresenter` (a
  fork of the upstream presenter, `new`-ed directly). This is a **UI type-replacement**, not a hook —
  cleanly inverting it means re-architecting upstream UI-controller selection. **User decision 2026-07:
  keep inline** (same category as the stylesheet forks). Its 3 upstream refs stay: `AdminbusTab.xaml.cs`,
  `CraftingUIController.cs`, `SandboxUIController.cs`.

**Phase 0 decoupling done for the server/client blockers:**
- **`PricingSystem`** (server) named `MedievalPhysicalPricedComponent` in two spots (entity + prototype
  material-price widening). Inverted via two upstream events in `Content.Shared/Cargo/PriceCalculationEvent.cs`:
  `GetPricedByCompositionEvent` (directed on entity) + `GetEstimatedPricedByCompositionEvent(EntityPrototype)`
  (broadcast for the prototype path). `MedievalPhysicalPricedSystem` answers both. Component now names-free
  in upstream → moved with its system.
- **`ClientClothingSystem`** (client) called `DirtVisualsSystem.TryAddEquipmentDirtLayer(...ref index)`.
  Inverted via `[ByRefEvent] AddEquipmentDirtLayerEvent` (in ClientClothingSystem.cs; carries the params +
  a mutable `Index` since a struct can't hold `ref int`). `DirtVisualsSystem` subscribes on `DirtableComponent`.
  Also needed `using Content.Shared.DisplacementMap;` in ClientClothingSystem (the type was only used via `var`
  before). `ProjectileSystem`/`PaperWindow` refs are downstream (to shared) → not blockers.

**Assembly-load wiring — the real work (skill doc's old open question, now solved):**
- **The cycle rule:** nothing may reference `Content.Respiral.Server/.Client` (they reference
  Content.Server/.Client → a back-reference would be circular). So they can't be auto-copied like
  `Content.Respiral.Shared` (which IS referenced by Content.Server/.Client and thus copied into their bins).
- **How the dll reaches the scan dir:** the engine mounts `/Assemblies/` → `bin/Content.Server/` (server) /
  `bin/Content.Client/` (client) in dev (`ProgramShared.DoMounts`, `ServerOptions/GameControllerOptions.
  ContentBuildDirectory`), then `ModLoader` scans it for `Content.*.dll`. So the two new csproj set
  **`<OutputPath>..\bin\Content.Server\</OutputPath>`** / `..\bin\Content.Client\` — their dll lands exactly
  where the scan looks. `<Private>false</Private>` on their ProjectReferences avoids re-copying the (already
  present) upstream dlls.
- **Build trigger:** nothing references them, so a plain `dotnet run --project Content.Server` won't build
  them — a **full solution build** (or building the Respiral project) does. Added both to `SpaceStation14.slnx`.
- **Test/tooling harness (explicit load, not prefix scan):** added markers `RespiralServerAssemblyMarker` /
  `RespiralClientAssemblyMarker`; registered in `PoolManager.Startup` (client & server lists) and
  `ContentUnitTest.GetContentAssemblies`. `Content.IntegrationTests` + `Content.Tests` csproj now
  ProjectReference both new assemblies (needed for the marker `typeof`s to resolve). YAMLLinter uses
  PoolManager → covered.
- **Packaging:** `ClientPackaging` hardcoded name array += `"Content.Respiral.Client"`; both packaging build
  steps now build the **Respiral** csproj (pulls upstream as a dep) instead of the upstream one, so the dll is
  produced. `ServerPackaging` copy list is `deps.json`-derived (won't contain the un-referenced Respiral.Server)
  → append `"Content.Respiral.Server"` manually.
- **GlobalUsings:** each new assembly needs its own `GlobalUsings.cs` mirroring the upstream project's (global
  usings don't flow across ProjectReferences).
- **`-t:Rebuild` quirk:** rebuilding `Content.Respiral.Client` via `/t:Rebuild` fails the nested
  `CompileRobustXaml` (MSB3073) — a build-graph race, not a real error; plain `dotnet build` (incl. clean
  from no obj/bin) succeeds. Use `dotnet build`, not Rebuild.

**Remaining in `Content.Server/_Respiral`:** only `ZLevels`. **In `Content.Client/_Respiral`:** `Controllers`
+ `ZLevels`.

**Next / deferred:** the `Controllers` UI-replacement (needs UI-selection re-architecture) and the stylesheet
forks (Phase 3) if full upstream-purity is ever wanted.

## Goal

Move all purely-additive Respiral code (everything under `*/\_Respiral/` and `*/\_CE/`) out of the
upstream `Content.Shared/.Server/.Client` projects into three new projects:

```
Content.Respiral.Shared  -> references Content.Shared
Content.Respiral.Server  -> references Content.Server, Content.Respiral.Shared
Content.Respiral.Client  -> references Content.Client, Content.Respiral.Shared
```

Dependency direction must be **one-way**: `Content.Respiral.* → Content.*`. Upstream must never
reference Respiral.

## Why the engine allows this (no engine fork needed)

The engine's ModLoader does **not** load a fixed list — it scans the assembly output directory for
DLLs whose filename starts with a prefix (`resourceManifest.AssemblyPrefix ?? "Content."`):

- `RobustToolbox/Robust.Client/GameController/GameController.cs:338` and
  `Robust.Server/BaseServer.cs:330` → `TryLoadModulesFrom(assemblyDir, assemblyPrefix)`.
- `RobustToolbox/Robust.Shared/ContentPack/ModLoader.cs:75` → picks every `*.dll` starting with the
  prefix.
- `RobustToolbox/Robust.Shared/ContentPack/BaseModLoader.cs:38` →
  `ReflectionManager.LoadAssemblies(...)` registers each assembly, so systems / components /
  prototypes are discovered by reflection across **all** loaded content assemblies.

Therefore `Content.Respiral.Shared.dll` etc. are auto-discovered by the `Content.` prefix. No engine
change, no manifest change (`Resources/manifest.yml` keeps default prefix). An entry-point class
(`GameShared`/`GameClient`/`GameServer`) is optional — systems load without one.

Sandboxing: each new assembly passes the same type-checker the current `_Respiral` code already
passes as part of `Content.Shared` (`AssemblyTypeChecker`, allowed prefixes).

## THE HARD BLOCKER: this cannot be done as a plain "move files"

There is upstream→Respiral coupling that makes the split fail to **compile** until removed. This is
Phase 0 and it is mandatory. It is also the most valuable part for code style on its own.

### Blocker 1 — Circular dependency: 13 upstream files reference `_Respiral`/`_CE`

`Content.Respiral.Shared` must reference `Content.Shared`. These upstream files reference back into
Respiral namespaces, which would make `Content.Shared → Content.Respiral.Shared → Content.Shared`
(circular, won't build):

```
Content.Shared/IdentityManagement/Identity.cs                      -> _Respiral.Identity.Components
Content.Shared/IdentityManagement/IdentitySystem.cs                -> _Respiral.Identity
Content.Shared/Clothing/EntitySystems/SharedChameleonClothingSystem.cs -> _Respiral.Dirt(.Components)
Content.Shared/Parallax/Biomes/SharedBiomeSystem.cs                -> _Respiral.Parallax.Biomes.Layers
Content.Shared/Throwing/ThrownItemSystem.cs                        -> _CE.ZLevels...
Content.Client/Clothing/ClientClothingSystem.cs                    -> _Respiral.Dirt
Content.Client/Paper/UI/PaperWindow.xaml.cs                        -> _Respiral.Skills (Literacy helper)
Content.Client/Parallax/ParallaxOverlay.cs                         -> _CE.ZLevels...
Content.Client/UserInterface/Systems/Crafting/CraftingUIController.cs -> _Respiral.Controllers
Content.Client/UserInterface/Systems/Sandbox/SandboxUIController.cs   -> _Respiral.Controllers
Content.Client/Administration/UI/Tabs/AdminbusTab/AdminbusTab.xaml.cs -> _Respiral.Controllers
Content.Server/Projectiles/ProjectileSystem.cs                    -> _Respiral.Dirt
Content.Server/Cargo/Systems/PricingSystem.cs                     -> _Respiral.Pricing.Components
```

Re-list at any time:
```bash
grep -rln "Content\.\(Shared\|Server\|Client\)\._\(Respiral\|CE\)" \
  Content.Shared Content.Server Content.Client --include=*.cs \
  | grep -v "/_Respiral/" | grep -v "/_CE/"
```

**Fix pattern per file** — remove the upstream→Respiral type name; invert the dependency so Respiral
subscribes to upstream instead:

- **Hooks that call a Respiral system** (e.g. `ProjectileSystem` → `_dirt.AddBloodDirtFromDamage`,
  `SharedChameleonClothingSystem`/`ClientClothingSystem` → dirt visuals): replace the direct call
  with a `RaiseLocalEvent` of a struct event **defined in upstream** (or reuse an existing upstream
  event), and move the handler into a Respiral system that `SubscribeLocalEvent`s. `ProjectileSystem`
  already raises `ProjectileHitEvent`; blood-dirt needs the computed `damage`, so subscribe to
  `DamageChangedEvent` instead.
- **`switch`/branch on a Respiral type** (`SharedBiomeSystem` → `BiomeRegionLayer` cases): this needs
  a polymorphic/virtual hook on the upstream layer base, or an event per layer processed. Harder —
  may need an upstream extension point (`virtual` method or event) that Respiral overrides.
- **UI controllers replacing upstream ones** (`CraftingUIController`, `SandboxUIController`,
  `AdminbusTab` → `_Respiral.Controllers.Medieval*`): upstream must not `new`/resolve the Medieval
  controller by name. Options: register the Respiral controller via reflection/IoC and have upstream
  resolve an interface, or gate via CVar. Verify each is reachable without the upstream file naming
  the type.
- **Reading Respiral components** (`Identity` → `KnownFacesComponent`, `PaperWindow` → skills/literacy,
  `PricingSystem` → `MedievalPhysicalPricedComponent`): upstream must raise an event that a Respiral
  handler answers (e.g. an `ev.Name`/`ev.Price` ref event the Respiral system fills), instead of
  `TryComp<RespiralComponent>` inline. NB: `PaperWindow`'s literacy read is now funneled through the
  single static `Content.Shared/_Respiral/Skill/Literacy.cs` (`Literacy.GetLevel(uid, entMan)`,
  2026-07) — that one call is the seam to convert to an upstream ref event; also used by
  `MedievalIdentitySystem` (Respiral-side, moves freely).
- **ZLevels** (`ThrownItemSystem`, `ParallaxOverlay` → `CESharedZLevelsSystem`): the deepest; see
  [[zlevels]]. Likely needs upstream events/hooks in the throw and parallax paths.

### Blocker 2 — 4 partial classes complete an upstream class across the boundary

`partial class` cannot span two assemblies. These Respiral/`_CE` partials extend an upstream class:

| Partial | Upstream side | Fix |
|---|---|---|
| `CCVars` | `Content.Shared/CCVar/CCVars*.cs` | ⏭️ **OUT OF SCOPE** — this is the `_CE` ZLevels CVars partial; `_CE` stays in upstream (2026-07 scope decision). Left as `partial CCVars`. |
| `ScalingViewport` | `Content.Client/Viewport/ScalingViewport.cs` | `_CE` adds `RenderZLevels`. Needs an upstream hook/event in the render path, or the Z-level render invoked via a subscribed event rather than a partial method. Hardest of the four. |
| `MobStateCondition` | `Content.Shared/EntityConditions/.../MobStateEntityConditionSystem.cs` | Check what the Respiral partial adds; likely re-expressible as a separate condition type/system. |
| `WhitelistCondition` | `Content.Server/Connection/Whitelist/WhitelistCondition.cs` | Same — separate type or upstream extension point. |

Re-list:
```bash
for f in $(grep -rln "partial class" Content.*/_Respiral Content.*/_CE --include=*.cs); do
  grep -oP "partial class \K\w+" "$f"; done | sort -u > /tmp/p.txt
while read c; do grep -rln "partial class $c\b" Content.Shared Content.Server Content.Client \
  --include=*.cs | grep -v "/_Respiral/" | grep -v "/_CE/" | grep -q . && echo "$c"; done < /tmp/p.txt
```
(243 Respiral partials total; only these 4 cross the boundary — the rest are Respiral-internal and
move freely.)

### Blocker 3 — `internal` access (minor)

Upstream projects currently `[assembly: InternalsVisibleTo]` only `Content.Tests` /
`Content.IntegrationTests` (`Content.Server/AssemblyInfo.cs`, `Content.Client/AssemblyInfo.cs`;
`Content.Shared` has none). If any moved Respiral code touches upstream `internal` members, add
`InternalsVisibleTo("Content.Respiral.*")` to the relevant upstream `AssemblyInfo.cs`. Prefer making
the member `public` upstream only if it is genuinely part of the intended API; otherwise
InternalsVisibleTo keeps the boundary honest.

## Execution plan (phased)

### Phase 0 — Decouple upstream → Respiral (MANDATORY GATE)
Scope: ~17 files (13 refs + 4 partials). No project changes yet; repo keeps building and stays
playable throughout. This is valuable on its own — upstream stops naming Respiral types.
1. For each of the 13 refs, apply the matching fix pattern above (event / interface / CVar gate).
2. Convert the 4 boundary partials.
3. After each change: build + run affected flow (see [[testing]]). Verify no
   `Content.*._Respiral`/`_CE` reference remains in upstream (`grep` above returns empty).
   Verify no cross-boundary partial remains.

### Phase 1 — Create empty assemblies
1. Add `Content.Respiral.Shared/.Server/.Client.csproj` (copy TFM, `Nullable`, analyzer/`RA0032`
   settings, `LangVersion` from the upstream csproj — see `.editorconfig` and existing csproj).
2. References: Shared→`Content.Shared`; Server→`Content.Server`+`Content.Respiral.Shared`;
   Client→`Content.Client`+`Content.Respiral.Shared`. Plus the same engine (`Robust.*`) refs the
   upstream projects use.
3. Add all three to `SpaceStation14.slnx`.
4. Ensure their build output lands in the same assembly dir the ModLoader scans (same `bin` output
   as `Content.Client`/`Content.Server`), so the `Content.` prefix picks them up.
5. Add `InternalsVisibleTo` upstream where Phase 0 revealed internal use.
6. Build. Empty assemblies should load at runtime (check the client/server log for
   `Found module '...Content.Respiral.*.dll'`).

### Phase 2 — Move the files
Decide the include strategy first — **this is a real gotcha**: SS14 content csproj files use glob
includes (`**/*.cs`) rooted at the project dir. Two viable layouts:
- **(a) Separate directories**: physically move `Content.Shared/_Respiral/**` →
  `Content.Respiral.Shared/**`, etc. Cleanest; the new csproj globs its own dir; the old projects no
  longer see the files. Must update `namespace`? No — namespaces are `Content.Shared._Respiral.*`
  today; keep them or rename to `Content.Respiral.Shared.*` (rename = touch every `using`, large but
  mechanical). **Recommend keeping namespaces initially** to minimize churn, rename later if wanted.
- **(b) Keep files in place, re-home via csproj**: have the upstream csproj `<Compile Remove>` the
  `_Respiral`/`_CE` trees and the new csproj `<Compile Include>` them by absolute/relative glob. Less
  file movement but fragile and confusing; **not recommended**.
Move `_CE/` (ZLevels, MIT) too — note the MIT-vs-GPL license boundary (see CLAUDE.md); a separate
assembly actually makes the license split cleaner. Consider a dedicated `Content.CE.*` set if strict
MIT isolation is wanted, or keep `_CE` inside `Content.Respiral.*` and preserve per-file headers.
After the move: build, run client+server, smoke-test Respiral systems (blacksmithing, skills, dirt,
identity, zlevels, biomes).

### Phase 3 — Optional cleanup (not required for the split)
- Fork the stylesheet (category A in [[upstream_patches]], 14 files) into a `RespiralStylesheet` in
  `Content.Respiral.Client`, registered via `IStylesheetManager`. Removes 14 inline `// Respiral-
  Stylization` edits from upstream style files.
- Optionally rename namespaces `Content.Shared._Respiral.*` → `Content.Respiral.Shared.*` for full
  consistency.

## What does NOT move (stays inline in upstream, forever)

The ~52 non-additive inline patches catalogued in [[upstream_patches]] (categories A–D: color
literals, localization constants, static-class/component field additions, deep method-body and
signature edits, `override`s). These are edits **inside** upstream types and cannot live in another
assembly. The split does **not** reduce merge conflicts in these files — keep them inline with
`// Respiral -` markers. This is expected, not a failure of the plan.

## Risk / effort summary

| Phase | Effort | Risk | Needs gameplay testing |
|---|---|---|---|
| 0 Decouple | High (design per file) | High — behavior-changing | Yes, every touched flow |
| 1 Scaffolding | Low | Low | Boot only |
| 2 Move files | Medium (mechanical) | Medium — build/glob/namespace | Yes, smoke test all systems |
| 3 Cleanup | Low–Medium | Low | Visual (styles) |

The `ScalingViewport` partial and the `SharedBiomeSystem` `switch` are the two hardest individual
items and should be spiked first — if they resist a clean upstream hook, that is the signal to
consider a **partial split** instead (extract only subsystems with zero upstream→Respiral coupling —
e.g. Blacksmithing, Wear, Lock, Flora — and leave the coupled ones in `Content.Shared/_Respiral`).

## Alternatives considered

- **Partial split** (only decoupled subsystems move; coupled ones stay in `Content.Shared/_Respiral`):
  faster, avoids the hardest refactors, but Respiral ends up in two locations — partially against the
  "clean code style" goal.
- **Do nothing / keep single assembly**: rejected — does not meet the maintainability goal.
- **Full split** (this plan): best end state for code style; gated by Phase 0.

Decision pending. When work starts, update this file's status line and check items off per phase.
