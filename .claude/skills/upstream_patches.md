# Upstream Patches

Map of all **inline edits to upstream SS14 files** (files outside `_Respiral/` directories that
carry a `// Respiral ...` / `// _Respiral ...` marker). This is the integration surface between
Respiral and the engine content: every entry here is a point that must be re-checked on an
upstream/engine merge.

## Why this file exists

A recurring question is whether `_Respiral` C# could be split into its own `.csproj`
(`Content.Respiral.Shared/.Server/.Client`). **Answer: not worth it.** The engine would load such
assemblies automatically (ModLoader scans the assembly dir for the `Content.` prefix — see
`engine_submodule.md` / `RobustToolbox/Robust.Shared/ContentPack/ModLoader.cs`), but ~52 of the 56
inline patches below **cannot** move to a separate assembly because they edit upstream method
bodies, method signatures, component fields, static classes, or stylesheets. Cross-assembly C#
cannot:

- add a field/`[DataField]` to an upstream component,
- add a member to an upstream `static` class,
- use `partial class` spanning assemblies,
- `override` a method registered on the upstream system,
- change an upstream public method signature.

A separate project would therefore **not** reduce merge conflicts (those live in the inline-patched
files) and would lose `internal` access. Keep patches inline with `// Respiral -` markers — that is
the optimal format for "patches on top of upstream".

## Audit snapshot (56 files)

Counts are approximate; the categorization is the point.

```
Cosmetic / style literals (A)          14   namertvo (movable only by forking the stylesheet)
Localization / config constants (B)     4   namertvo
Extends upstream static/component (C)   8   namertvo
Deep method body / signature edits (D) ~27  namertvo
Extractable via events (E)             ~4   movable with rework, leaves 1-3 line residue
```

### A — Cosmetic: color/style literals inside upstream style files (14)
`Content.Client/Stylesheets/StyleBase.cs`, `StyleNano.cs`, `StyleSpace.cs`,
`Administration/UI/CustomControls/HSeparator.cs`, `VSeparator.cs`, `PlayerListControl.xaml.cs`,
`Administration/UI/Tabs/ObjectsTab`, `PlayerTab`, `Info/PlaytimeStats/PlaytimeStatsWindow.cs`,
`Lobby/UI/CharacterSetupGui.xaml.cs`,
`UserInterface/Systems/Chat/Controls/ChannelFilterButton.cs`,
`Content.Shared/Chat/ChatChannelExtensions.cs`,
`UserInterface/Systems/Alerts/Controls/AlertControl.cs` (alert icon MaxSize 96x96),
`Content.Server/Station/Components/StationBiomeComponent.cs` (default `MapLightColor`).

Tagged `// Respiral-Stylization`. Overwrite specific literals. **Only movable group among the
namertvo ones**: SS14 stylesheets are pluggable via `IStylesheetManager`, so all of A could collapse
into one `RespiralStylesheet` in a separate file. Best ROI cleanup — removes 14 conflict points.

### B — Localization / config constants (4)
`Content.Shared/Localizations/ContentLocalizationManager.cs` (culture `ru-RU` + `MANY` fn),
`Content.Shared/Preferences/HumanoidCharacterProfile.cs` (cyrillic name regex),
`Content.Shared/Chat/SharedChatSystem.cs` (LOOC `_` / emote `%` prefixes),
`Content.Server/Chat/Managers/ChatSanitizationManager.cs`.
See also `localization.md`. Namertvo (const/static inside upstream types).

### C — Extends upstream static class / component (8)
`Content.Shared/Input/ContentKeyFunctions.cs` (`OfferItem`),
`Content.Shared/Verbs/VerbCategory.cs` (`Interaction`),
`Content.Client/Input/ContentContexts.cs` (adds `OfferItem` to human context),
`Content.Client/Options/UI/Tabs/KeyRebindTab.xaml.cs` (adds rebind button),
`Content.Shared/Light/Components/ImplicitRoofComponent.cs` (`Disabled` field) + consumer
`Content.Client/Light/RoofOverlay.cs`,
`Content.Shared/Light/Components/LightCycleComponent.cs` (`Duration`, `InitialOffset`) + consumer
`Content.Server/Light/EntitySystems/LightCycleSystem.cs`,
`Content.Client/Lathe/LatheSystem.cs` (`LatheVisualLayers.RunningUnlit` enum member + a visibility
toggle for that layer gated on `IsRunning`, for the power-free `MedievalSmelter` fire overlay — see
`blacksmithing.md`).
Namertvo — cannot extend an upstream static class or component from another assembly.

### D — Deep edits to upstream method bodies / signatures (~27)
- **ZLeveling** (11): `Content.Client/Viewport/ScalingViewport.cs`, `Parallax/ParallaxOverlay.cs`,
  `Damage/Systems/StaminaSystem.cs`, `Overlays/StencilOverlay.Weather.cs`,
  `Content.Shared/Throwing/ThrownItemSystem.cs`, `Movement/Systems/SharedMoverController.Input.cs`,
  `Maps/ContentTileDefinition.cs`, `Content.Server/NPC/Pathfinding/PathfindingSystem.{cs,Common,Grid}.cs`,
  `Content.Shared/NPC/PathfindingBreadcrumb.cs`. See `zlevels.md`.
- **Biome / ProcGen** (4): `Content.Server/Parallax/BiomeSystem.cs`
  (⚠ **public `EnsurePlanet` signature changed** — added `lightOffset`, `mapLight` params),
  `Content.Shared/Parallax/Biomes/SharedBiomeSystem.cs` (new `switch` cases for `BiomeRegionLayer`),
  `Content.Server/Station/Systems/StationBiomeSystem.cs`, `LightCycleSystem.cs`. See `biomes.md`.
- **Identity** (2): `Content.Shared/IdentityManagement/Identity.cs`, `IdentitySystem.cs`
  (control-flow inside methods: known faces see through anon, public identity always anon).
  See `identity.md`.
- **Chat** (1): `Content.Server/Chat/Systems/ChatSystem.cs` — `SendMessageToOne` is an `override`
  of the abstract base → can only live in the class.
- **Fax** (2): `Content.Server/Fax/FaxSystem.cs` `Update()` — query relaxed from
  `<FaxMachineComponent, ApcPowerReceiverComponent>` to `<FaxMachineComponent>` with a
  conditional `TryComp<ApcPowerReceiverComponent>` power check, so the powerless medieval
  `MedievalTeleportTable` animates/prints. `Content.Shared/Fax/Components/FaxMachineComponent.cs` —
  `PrintingTime` changed from `[ViewVariables]` to `[DataField]` so the teleport table can set
  `printingTime` to the arrival-sound length. See `round_goal.md`.
- **Pricing** (2): `Content.Server/Cargo/Systems/PricingSystem.cs`, `Armor/ArmorSystem.cs`
  (branch on `PhysicalComposition` / `MedievalPhysicalPricedComponent`). See `economy.md`.
- **UI / misc** (7): `Content.Client/Items/Systems/ItemSystem.cs` (HeldPrefix),
  `Paper/UI/PaperWindow.xaml.cs` (char substitution by literacy — see `skill_system.md`),
  `MainMenu/UI/MainMenuControl.xaml.cs` (menu logo),
  `Administration/UI/Tabs/AdminbusTab/AdminbusTab.xaml.cs`,
  `UserInterface/Systems/Sandbox/SandboxUIController.cs`,
  `UserInterface/Systems/Crafting/CraftingUIController.cs` (Medieval spawner/craft controllers hide
  upstream ones), `Parallax/ParallaxControl.cs` (load-screen scroll).

All namertvo — control-flow, signatures, or overrides inside upstream types.

### E — Extractable via events (~4, movable with rework)
| File | Hook | Extraction path |
|---|---|---|
| `Content.Server/Projectiles/ProjectileSystem.cs` | `_dirt.AddBloodDirtFromDamage` | upstream already raises `ProjectileHitEvent`; blood-dirt needs computed `damage`, so subscribe to `DamageChangedEvent` in a _Respiral system |
| `Content.Shared/Tools/Systems/SharedToolSystem.cs` | `ToolUsingFinishedEvent` (`// Respiral-Wear`) | `[ByRefEvent]` struct (now carries `bool Cancelled`) + `RaiseLocalEvent(uid, ref …)` in `OnDoAfter` stay upstream. Subscriber is `AddWearAfterUseSystem` (`WearTrigger.Tool`, see [[skill_system.md]]) — adds tool-use wear (e.g. hammer building). The 3 residual lines can't leave upstream (raise site is inside the upstream method body) |
| `Content.Client/Clothing/ClientClothingSystem.cs` | `DirtVisualsSystem` dirt layer | injected mid layer-build; verify a suitable equip/appearance event exists first |
| `Content.Shared/Clothing/EntitySystems/SharedChameleonClothingSystem.cs` | dirt hook | subscribe to equip/appearance event |

Even here 1-3 residual lines remain upstream unless the engine already raises a fitting event.

### TTS (Corvax port) — 3 hooks, tag `Corvax-TTS`

- `Content.Server/Entry/EntryPoint.cs` — `[Dependency] TTSManager` + `_ttsManager.Initialize()`.
- `Content.Server/IoC/ServerContentIoC.cs` — `deps.Register<TTSManager>()`.
- `Resources/Prototypes/Body/species_base.yml` — `- type: TTS` on `BaseSpeciesMobOrganic`.

All three are additive (new lines only), wrapped in `// Respiral-Start Corvax-TTS` /
`# Respiral-Start Corvax-TTS` … `-End`. See [[tts.md]].

## Marker conventions in use

- `// Respiral-Start` / `// Respiral-End` (or `-start` / `-end`) — wraps a multi-line block.
- `// Respiral-<Tag>` inline — single-line edit; tags seen: `Stylization`, `Localization`,
  `ZLeveling` / `ZLeveling-Edit`, `ProcGen` / `ProcGen-Edit`, `Dirtable`, `Durability`, `Offer`,
  `Interactions`, `Change`, `Add`, `edit-<reason in RU>`.
- `_Respiral-...` variant appears in Server pricing/pathfinding/armor edits.
- To re-list all patches: `grep -rln "Respiral" Content.{Shared,Server,Client} --include=*.cs | grep -v "_Respiral/"`.

## Recommendation

1. Fork stylesheet (category A) into a `RespiralStylesheet` — removes 14 conflict points cleanly.
2. Convert category E to event subscriptions — shrinks those inline sites to a minimal residue.
3. Keep everything else inline with `// Respiral -` markers. Do **not** split `_Respiral` C# into a
   separate `.csproj` — it does not reduce merge conflicts and loses `internal`/`partial` access.
