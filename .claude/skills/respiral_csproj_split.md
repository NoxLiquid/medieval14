# Respiral csproj Split (Planned / Future Work)

> **Status: NOT DONE — planned future refactor.** This file is the design + execution guide for
> extracting Respiral C# into its own assemblies (`Content.Respiral.Shared/.Server/.Client`).
> Goal is not to fix a bug but **long-term maintainability and a clean, explicit code style**:
> additive Respiral code isolated in its own assembly with a one-directional dependency on upstream.
>
> Read together with [[upstream_patches]] (the inline-patch map) and [[engine_submodule]] (how the
> engine loads content assemblies).

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
Content.Client/Paper/UI/PaperWindow.xaml.cs                        -> _Respiral.Skills(.Components/.Prototypes)
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
  `TryComp<RespiralComponent>` inline.
- **ZLevels** (`ThrownItemSystem`, `ParallaxOverlay` → `CESharedZLevelsSystem`): the deepest; see
  [[zlevels]]. Likely needs upstream events/hooks in the throw and parallax paths.

### Blocker 2 — 4 partial classes complete an upstream class across the boundary

`partial class` cannot span two assemblies. These Respiral/`_CE` partials extend an upstream class:

| Partial | Upstream side | Fix |
|---|---|---|
| `CCVars` | `Content.Shared/CCVar/CCVars*.cs` | Respiral CVars do **not** need to be part of upstream `CCVars`. Move them to a new non-partial `RespiralCVars` static class in `Content.Respiral.Shared`. `CVarDef` works from any assembly. Easy. |
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
