# Tabard attachment (знак дома на броне)

Attach a **tabard** (house surcoat) onto clothing that carries a "tabard-attachable" flag (mostly armor).
The tabard's `equipped-TABARD` overlays the worn clothing on the wearer, and its `icon` overlays the
clothing's inventory icon. Attach = use the tabard entity on the clothing → DoAfter. Detach = context-menu
**Verb** «Снять табард» (pops the tabard item back).

## Scope
Mechanics + visuals + prototypes + **identity affiliation** (2026-08). Flag on the **5 armor pieces** only.
One tabard: **Королевство ворона** (`crow_faction.rsi`). NpcFaction (MedievalTownPeople) still out of scope.

## Files
- **Components** `Content.Shared/_Respiral/Tabard/Components/`:
  - `MedievalTabardComponent` (on the tabard item): `Rsi` (ResPath, required), `EquippedState`
    (`equipped-TABARD`), `IconState` (`icon`), `House` (`ProtoId<HouseSealPrototype>?`, unused now),
    `AttachTime` (2s).
  - `MedievalTabardAttachableComponent` (flag on clothing; `AutoGenerateComponentState(true)` — needed for the
    client's `AfterAutoHandleStateEvent`): container id const `tabard-slot` + **networked visual cache**
    `RsiPath`/`EquippedState`/`IconState` (all null = nothing attached). The attached tabard entity lives in
    the `tabard-slot` `ContainerSlot`, so detach returns a real item.
- **Event** `Content.Shared/_Respiral/Tabard/MedievalTabardDoAfterEvent.cs` (`SimpleDoAfterEvent`) — stays
  Shared (networked).
- **Logic** `Content.Server/_Respiral/Tabard/MedievalTabardSystem.cs` — **server-only** (no shared logic: the
  client only reads component fields to render, so there's nothing to share; no `SharedMedievalTabardSystem`
  base). Handles `AfterInteractEvent` (tabard→clothing, starts DoAfter), `MedievalTabardDoAfterEvent` (insert
  into container + fill the visual cache + `Dirty`), `GetVerbsEvent<Verb>` (detach: `Remove` +
  `_hands.PickupOrDrop` + clear cache). DoAfter/verb/container are all fine server-side; visuals sync via the
  networked cache. Attach/spawn both funnel through one helper `SetTabardVisuals(ent, tabardComp)`.
- **Spawn-with-tabard (livery, 2026-08)** — same server system:
  - `ComponentInit` → `EnsureContainer<ContainerSlot>(tabard-slot)` so the slot exists **before** MapInit
    (upstream `ContainerFill` uses `TryGetContainer`, it does NOT create the container).
  - `MapInit` (`after: [typeof(ContainerFillSystem)]`) → if the `tabard-slot` holds a `MedievalTabardComponent`,
    `SetTabardVisuals` from it + refresh wearer. So a piece spawned with a tabard already inside renders the
    overlay/affiliation with **no manual attach**, and the tabard is a **real removable item** (detach verb works).
  - Livery variants in `armor.yml` (`MedievalClothingOuterArmor{QuiltedGambeson,IronChainmail}Crow`,
    `HideSpawnMenu`) just add `- type: ContainerFill { containers: { tabard-slot: [ MedievalTabardCrowKingdom ] } }`
    on top of the plain armor (which already carries the `MedievalTabardAttachable` flag). Given to guard/knight
    starting gear. **Do not** set the visual cache in YAML — it's `[ViewVariables]`, filled only at runtime.
- **Client visuals** `Content.Client/_Respiral/Tabard/MedievalTabardVisualsSystem.cs`:
  - equipped overlay — subscribes `GetEquipmentVisualsEvent` **on its own `MedievalTabardAttachableComponent`**
    with `after: [typeof(ClothingSystem)]`, appends `PrototypeLayerData { RsiPath, State = EquippedState }`
    keyed `$"{slot}-tabard"`. **No upstream `ClientClothingSystem` edit needed** — the duplicate-subscription
    rule ([[inhand_icon]]) is per (component, event); subscribing on a DIFFERENT component is fine (same trick
    upstream's `FlippableClothingVisualizerSystem` uses).
  - icon overlay — on `AfterAutoHandleStateEvent`/`ComponentStartup`, add/remove a `medieval-tabard-icon`
    layer on the clothing's OWN `SpriteComponent` (`SpriteSystem.AddLayer`/`RemoveLayer(key)`).
  - refresh — on state change calls `SharedItemSystem.VisualsChanged(owner)` to re-run the equipped visuals
    (mirrors `FlippableClothingVisualizerSystem.OnFolded`).
- **Assets/YAML:**
  - CREATED `Textures/_Respiral/Clothing/Tabards/crow_faction.rsi/meta.json` (`icon` 1-dir + `equipped-TABARD`
    4-dir; the PNGs existed, meta.json didn't).
  - `Entities/Clothing/tabard.yml` → `MedievalTabardCrowKingdom` (parent `MedievalBaseClothing`,
    `- type: MedievalTabard` with `rsi:` + `house: HouseSealCrown`).
  - `Entities/Clothing/outer/armor.yml` → `- type: MedievalTabardAttachable` on the 5
    `MedievalClothingOuterArmor{IronChainmail,QuiltedGambeson,LeatherJacket,Plate,PlateLeather}`.
  - Recipe `MedievalTabardCrowKingdomRecipe` (Tailor Understanding, cloth 100) in `mantle.yml` + Tailor pack.
  - Locale `Locale/ru-RU/_Respiral/tabard.ftl` (verb + popups).

## Sprite structure of tabard RSIs
`icon` (32×32) + `equipped-TABARD` (64×64 = 4 dirs). `tabard.rsi` also has `colored_*` (recolorable base for
future houses); `long_grayscale.rsi` has a grayscale base + 6 emblem overlay states
(clover/diamond/grail/heartch/hourglass/trident) — groundwork for more houses (not prototyped yet).

## Identity affiliation (IMPLEMENTED 2026-08)
Worn armor with an attached house tabard grants an affiliation shown as `PresumedJob` (visible only when the
face is hidden — the engine's rule). Design:
- `MedievalTabardComponent.Affiliation` (`LocId?`) — the affiliation the tabard grants (crow:
  `medieval-tabard-affiliation-crow` = «подданный Королевства ворона»).
- Cached (networked) into `MedievalTabardAttachableComponent.Affiliation` (string) on attach, cleared on detach
  — so the identity read needs no container traversal and works on both sides.
- **`MedievalTabardIdentitySystem`** (SHARED — identity computation is shared, mirrors the shared credential
  handler): subscribes `ModifyPresumedIdentityEvent` on **`InventoryComponent`** (≠ the identity system's
  `IdentityComponent` handler → no duplicate-subscription clash), iterates worn slots, and sets
  `args.Representation.PresumedJob` from the first attached tabard's cached affiliation.
- **Refresh:** attach/detach on worn armor won't fire an equip event, so `MedievalTabardSystem.RefreshWearer`
  calls `IdentitySystem.QueueIdentityUpdate(wearer)` (wearer = clothing's `Transform.ParentUid` if it has
  `IdentityComponent`) to recompute immediately.
- Interplay with the closed hoods ([[clothing]] IdentityBlocker): a hood/mask hides the face → the name goes
  anonymous → the tabard's `PresumedJob` then shows «средних лет подданный Королевства ворона мужчина».

## Future / not done
- Personal name / seal validation via the tabard's `House` (`HouseSealPrototype`) — the field is carried but
  not yet used for Model-B credential checks. See [[identity]].
- More houses (recolor `tabard.rsi` / emblem `long_grayscale.rsi`); tie `House` to a real house set.
- NpcFaction (MedievalTownPeople) membership — explicitly out of scope (identity/visual only).

## Verify
`dotnet build` clean (done). In-client: spawn `MedievalTabardCrowKingdom` + an armor piece → use tabard on
armor (DoAfter) → icon overlay on the armor icon; equip → `equipped-TABARD` on all 4 facings; right-click →
«Снять табард» → overlay gone, tabard back in hand.
