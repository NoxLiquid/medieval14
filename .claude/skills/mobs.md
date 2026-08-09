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

## Falling over on death/crit — RotationVisuals (pure YAML, no code)

`RotationVisualsComponent` (`Content.Shared/Rotation/RotationVisualsComponent.cs`) is a fully
data-driven "rotate sprite when downed" component — no custom C# needed to use it on a prototype.

Fields (YAML `type: RotationVisuals`): `defaultRotation` (deg, default 90 — also becomes
`horizontalRotation`'s starting value), `verticalRotation` (default 0, "standing" angle),
`horizontalRotation` (default 90, "lying down" angle), `animationTime` (default 0.125s, tween length).

Usage examples in repo:
- `Resources/Prototypes/Entities/Mobs/base.yml` (`BaseControllable`, generic mob root):
  `defaultRotation: 0, horizontalRotation: 0` — i.e. rotation disabled by default for generic mobs
  (they rely on `DamageStateVisuals` dead sprite swap instead).
- `Resources/Prototypes/Body/species_base.yml` (`BaseSpeciesMob`, base for ALL humanoid species):
  `defaultRotation: 90, horizontalRotation: 90` — **this is the actual "humanoid falls over" mechanic**.
  No `_Respiral` prototype currently overrides/adds `RotationVisuals`.

Trigger chain (fires regardless of whether `DamageStateVisualsComponent` exists on the entity):
`MobStateComponent` state change → `MobStateSystem.Subscribers.cs` `OnStateEnteredSubscribers`
(`Content.Shared/Mobs/Systems/MobStateSystem.Subscribers.cs:118-129`) calls local `Down(target)` for
Critical/Dead → `Down()` calls `_standing.Down(target)` → `StandingStateSystem.Down`
(`Content.Shared/Standing/StandingStateSystem.cs:118`) sets appearance data
`_appearance.SetData(uid, RotationVisuals.RotationState, RotationState.Horizontal, ...)`.
Client `RotationVisualizerSystem` (`Content.Client/Rotation/RotationVisualizerSystem.cs`) subscribes to
`AppearanceChangeEvent` on `RotationVisualsComponent` and tweens `SpriteComponent.Rotation` via an
`Animation`/`AnimationPlayerComponent`. Reverting to Alive → `StandingStateSystem.Stand` (line 164) sets
`RotationState.Vertical`.

**Independent of `DamageStateVisualsComponent`:** that component/visualizer lives entirely separately
in `Content.Client/DamageState/` and reacts to a *different* appearance key (`MobStateVisuals.State`,
set directly in `OnStateEnteredSubscribers`). A mob can have `RotationVisuals` with NO `dead` sprite
state at all (no `DamageStateVisualsComponent`) and will still visually "fall over" via rotation alone —
confirmed by `BaseControllable` setting rotation to 0/0 (opt-out) while relying on dead sprites instead,
proving the two systems are toggled independently per-prototype.

**Requirement:** entity needs `StandingStateComponent` (from `BaseMob`) + `MobStateComponent` — both
already present on any mob built from the standard `BaseMob`/`BaseSpeciesMob` chain. Purely additive
YAML: add `- type: RotationVisuals` with your angles, no C# required.

## Mounts & saddle slot (MedievalMobHorse)

File: `Resources/Prototypes/_Respiral/Entities/Mobs/Flora/Town/horse.yml`. Sprite: `_Respiral/Mobs/Flora/mount.rsi` (states `alive`/`harness`, 4 directions each — **no `dead`/`dead_harness` states**, death is rotation-only, see below).

**Riding:** uses the pre-existing but previously-unused `Content.Shared/_Respiral/Mount/` system
(`MountableComponent`, `RiderComponent`, `SharedMountableSystem.cs`, class `SharedMountSystem`) — this
horse is the first prototype to wire it up. Riding is implemented on top of vanilla `Strap`/`Buckle`:
`- type: Strap` (`position: Stand`) + `- type: Mountable`. `SharedMountSystem` relays rider input to the
mount and multiplies the mount's own `MovementSpeedModifierComponent` speed by `MountedSpeed` (1.5x
default) while ridden — **the mount itself needs an explicit `MovementSpeedModifier` component**, or the
speed boost silently no-ops (`TryComp` fails and the handler returns early).

**Saddle slot — deliberately NOT `ItemSlots`/`EntityStorage`, uses the standard Inventory+Clothing system:**
first attempt used `ItemSlotsComponent` + a custom `EntitySystem` bridging slot-fill to appearance data
(see [[containers]] "EntityStorage as single-item slot" for why `ItemSlots` needs a code bridge — it
does not auto-emit appearance data on insert/remove, unlike `EntityStorageComponent`). User pushed back
twice (wanted zero C#) and the actually-idiomatic answer was to reuse the vanilla clothing/inventory
system, exactly like pet mobs already do (`hamster_inventory_template.yml`, `pet_inventory_template.yml`,
`monkey_inventory_template.yml` etc. — non-humanoid mobs with a custom minimal `inventoryTemplate` is a
long-established vanilla pattern, see [[inventory]]). Final shape, 100% YAML, zero custom code:
- `- type: Inventory` (`speciesId: horse`, `templateId: medievalHorse`) + parent `StripableInventoryBase`
  (gives `Strippable` + the stripping UI window, so a player can drag the saddle into the slot; `Clothing.QuickEquip`
  (default `true`) also allows one-click-with-item-in-hand style equip).
- Saddle item (`MedievalSaddle`) is a normal `- type: Clothing` with `clothingVisuals.saddle` pointing at
  `mount.rsi` state `harness` — see [[inventory]] for the exact `SlotFlags` reuse + blacklist trick needed
  to stop the saddle being wearable by humans.
- Because both `alive` and `harness` are full-body composite art (not a small icon overlay), the
  equipped clothing layer is left with NO offset and no explicit `map:` bookmark on the horse's base
  `Sprite` — `ClientClothingSystem` then just `LayerMapReserve`s a new layer appended after the body
  layer, which visually fully occludes it (see [[inventory]] "Clothing draw order" — no bookmark → layer
  goes to the end of the list, which here is exactly what we want, a full cover-up not a small addition).

**Bugs found/fixed after real playtesting (2026-08):**
1. `OnRiderMove` (manual `_transform.SetLocalPositionNoLerp(rider, mountXform.LocalPosition + offset)`)
   was deleted — vanilla `Buckle.Buckle()` (`Content.Shared/Buckle/SharedBuckleSystem.Buckle.cs:369-371`)
   already reparents the buckled entity onto the strap (`ParentUid = strap`) at `strap.Comp.BuckleOffset` —
   ECS parenting alone keeps the rider following the mount for free. The manual code mixed coordinate
   spaces (added the mount's OWN local position — relative to ITS parent, the grid — to what should've
   been a small mount-relative offset) AND fought `SharedBuckleSystem.BuckleTransformCheck`
   (`SharedBuckleSystem.Buckle.cs:118-142`), which **auto-unbuckles** if `LocalPosition` drifts more than
   `1e-5` from `strap.BuckleOffset` on every `MoveEvent` — i.e. it was self-sabotaging every frame.
2. `RemComp<ActiveNPCComponent>`/`AddComp(..., new ActiveNPCComponent())` in `SharedMountSystem`
   (`Content.Shared`, runs on both sides) raced against `NPCSystem.WakeNPC`/`SleepNPC`
   (`Content.Server/NPC/Systems/NPCSystem.cs:111-143`, Server-only, wired to `MobStateChangedEvent` via
   `HTNSystem.cs:33`), which independently `EnsureComp`/`RemComp`s the same component on every mob-state
   transition. Crash symptom: `AddComponentInternal` throws "Component reference type ActiveNPC already
   occupied" on dismount, because `NPCSystem` had already re-added it. Live symptom: rider "can't control
   the horse" — `NPCSteeringSystem` kept driving the horse's `InputMoverComponent` via HTN because the AI
   was never durably asleep, fighting the rider's `_mover.SetRelay` input.
   **First fix attempt failed too:** a separate Server-only system independently subscribing to the same
   `<MountableComponent, StrappedEvent>`/`UnstrappedEvent` pairs crashed the server on boot with
   `InvalidOperationException: Duplicate Subscriptions for comp=..., event=StrappedEvent`
   (`Robust.Shared/GameObjects/EntityEventBus.Directed.cs:418` — `_eventSubsUnfrozen[compType].TryAdd(...)`
   is a plain dictionary keyed by `(component, event)`, so Robust allows **exactly one** `SubscribeLocalEvent`
   handler per that pair, globally, not "first system wins, others queue"). **Real fix:** `SharedMountSystem`
   became `abstract` (mirrors `SharedBuckleSystem`'s own pattern) with `protected virtual void
   OnRiderMounted(EntityUid mount)` / `OnRiderDismounted(EntityUid mount)` hooks called from the existing
   `OnStrapped`/`OnUnstrapped` (positioned *before* the `TryComp<MovementSpeedModifierComponent>` early
   `return`, otherwise a mount without that component would silently skip the NPC sleep/wake too).
   `Content.Server/_Respiral/Mount/MountSystem.cs : SharedMountSystem` overrides the hooks to call
   `NPCSystem.SleepNPC`/`WakeNPC` (server-only `NPCSystem` type, can't be referenced from Shared).
   `Content.Client/_Respiral/Mount/MountSystem.cs : SharedMountSystem` is an otherwise-empty concrete
   subclass — **mandatory**, since making the Shared class abstract means it's no longer instantiated by
   itself; each side (`Content.Server`/`Content.Client`) needs its OWN concrete `: SharedXSystem` subclass
   registered via reflection, or that side loses the shared logic (buckle/relay handling) entirely. This is
   the standard SS14 split-system pattern (cf. `SharedBuckleSystem` abstract + `Content.Server/Buckle/Systems/BuckleSystem.cs` +
   `Content.Client/Buckle/BuckleSystem.cs`), which additionally subscribe to their OWN, non-overlapping
   events (client's `BuckleSystem` adds `AppearanceChangeEvent`/`BuckledEvent`/etc, never re-subscribing to
   `StrappedEvent`) — that's the general answer any time Shared logic needs a server-only or client-only
   side effect: either subscribe to a *different* event in the platform-specific subclass, or (if it must
   hook the exact same event Shared already owns) add a `protected virtual` hook the Shared handler calls.
3. Copy-paste bug in `OnMobStateChanged(Entity<MountableComponent>, ...)`: **both** the Alive and
   non-Alive branches called `_buckle.TryUnbuckle` unconditionally whenever `Rider != null` — meaning the
   mount got force-dismounted even when transitioning back to Alive (e.g. healed while ridden). Fixed to
   only unbuckle in the non-Alive branch.

Lesson for any future `Content.Shared` system: adding/removing a component that has a Server-only
authority system already managing its lifecycle (check for existing `EnsureComp`/`RemComp` call sites on
that component first) will race with it. If in doubt, look for a Server-only setter/wrapper method (like
`NPCSystem.WakeNPC`/`SleepNPC`) instead of raw `AddComp`/`RemComp`.

4. **"Rider can't control the horse" even after the ActiveNPC fix — real root cause: `Buckle` itself
   blocks movement.** `SharedBuckleSystem.Buckle.cs:176-179` `OnBuckleUpdateCanMove` cancels
   `UpdateCanMoveEvent` whenever `BuckleComponent.Buckled == true` (correct for ordinary furniture — you
   shouldn't be able to just walk off a chair). But `SharedMoverController.Input.cs:308-340`
   `HandleDirChange` — the function that a keypress actually goes through — only forwards to
   `RelayInputMoverComponent`'s target if `entity.Comp.CanMove` is **true**:
   ```csharp
   if (entity.Comp.CanMove && RelayQuery.TryComp(entity, out var relayMover))
   {
       ...
       HandleDirChange(relayMover.RelayEntity, dir, subTick, state);
       return;
   }
   ```
   So a buckled rider's `CanMove` was `false` (from Buckle) and their WASD never even reached the relay
   check — `_mover.SetRelay` itself was wired correctly the whole time, the key presses just got dropped
   one step earlier. Fix: `SharedMountSystem` now also subscribes `<RiderComponent, UpdateCanMoveEvent>`
   with `after: [typeof(SharedBuckleSystem)]` and calls `args.Uncancel()` — since `ActionBlockerSystem
   .UpdateCanMove` (`Content.Shared/ActionBlocker/ActionBlockerSystem.cs:36`) raises ONE `UpdateCanMoveEvent`
   broadcast to every component the entity currently has with a handler, ordering between the two handlers
   (Buckle's cancel, ours un-cancel) determines the final `Cancelled` value. **Timing gotcha:** Buckle's own
   `SetBuckledTo` (`SharedBuckleSystem.Buckle.cs:208`) calls `ActionBlocker.UpdateCanMove` BEFORE
   `StrappedEvent` is raised — i.e. before our `RiderComponent` even exists yet — so that first recompute
   always cancels regardless of our new handler. `OnStrapped`/`OnUnstrapped` therefore each call
   `_actionBlocker.UpdateCanMove(args.Buckle.Owner)` a second time, right after adding/removing
   `RiderComponent`, to force a fresh recompute that our handler actually participates in.
   General lesson: `UpdateCanMoveEvent`'s doc comment says exactly this ("if you subscribe to this you
   must also call `ActionBlockerSystem.UpdateCanMove` both when preventing AND allowing movement again") —
   easy to miss since the *symptom* (nothing happens on keypress) gives no exception/log to point at it.

5. **"AI still fights for control while ridden" even after `SleepNPC` — `NPCOptimizationSystem` re-wakes
   it within ~2.5s.** `Content.Server/_Respiral/NPC/Systems/NPCOptimizationSystem.cs:89-104` `ProcessNPC`
   runs on a timer (`Cooldown = 2.5s`) over every `HTNComponent` mob and calls `_npc.WakeNPC(uid)` whenever
   there's a non-ghost `ActorComponent` (player) within `_activationRadius` (~21 tiles, `CCVars.ViewportMaximumWidth`) —
   a perf optimization so distant NPCs don't run HTN/steering. A rider standing on the mount is about as
   "nearby" as it gets, so this silently undoes `MountSystem.OnRiderMounted`'s `SleepNPC` call within one
   cooldown tick, and HTN resumes wandering/re-registers `NPCSteeringComponent`, fighting the relayed
   input. The system already has an opt-out: `NPCIgnoringOptimizeComponent` (marker,
   `Content.Server/_Respiral/NPC/Components/`) — `Update()`'s query skips any entity that has it
   (`NPCOptimizationSystem.cs:61`). Fix: `MountSystem.OnRiderMounted` also does
   `EnsureComp<NPCIgnoringOptimizeComponent>(mount)`; `OnRiderDismounted` does
   `RemComp<NPCIgnoringOptimizeComponent>(mount)` so normal proximity-based sleep/wake resumes once nobody
   is riding. General lesson: when disabling a *server-authoritative, periodically-reasserted* subsystem
   (anything with its own `Update(float frameTime)` loop that re-derives state from world conditions —
   proximity checks, timers, "is a player nearby" heuristics), a one-shot `RemComp`/state flip at the
   moment of the triggering event isn't enough if something else's `Update()` loop will just recompute and
   re-apply that same state on its own cadence; look for (or add) an explicit opt-out component/flag that
   that system's loop itself respects, rather than fighting it every frame.

6. **"Small teleport-back on stopping while riding" — NOT a Mount bug, accepted as a known engine
   limitation (2026-08 decision: leave as-is).** This is Robust's client prediction reconciliation
   (`RobustToolbox/Robust.Client/GameStates/ClientGameStateManager.cs` `ResetPredictedEntities` +
   `PredictTicks`, runs every network tick) rendering a genuine misprediction with **zero smoothing** —
   `TransformSystem.ActivateLerp` (`Robust.Client/GameObjects/EntitySystems/TransformSystem.cs:45-117`)
   only lerps the newest first-time-predicted tick and its own doc comment says it "generally assumes the
   client does not mispredict." Friction-based deceleration (`SharedMoverController.cs` `FrictionNoInput`)
   is exactly where the replayed trajectory becomes sensitive to the exact reset baseline (which shifts
   every `ApplyGameState`), so the snap is most visible right when movement keys are released. Confirmed
   NOT specific to Buckle/relay: `SetRelay` correctly marks both rider and mount as predicted
   (`SharedMoverController.Relay.cs:18-26`, re-asserted on every state apply, no ordering gap), and
   `SharedMechSystem.SetupUser` (`Content.Shared/Mech/EntitySystems/SharedMechSystem.cs:124-145`) does the
   *exact same* bare `_mover.SetRelay` with zero extra stabilization — so vanilla Mech piloting almost
   certainly has the identical latent issue, just less noticeable (slower accel/decel, third-person view,
   pilot invisible instead of a separate visible mount sprite). Also matches a documented, still-open
   engine TODO for collision/contact misprediction (`RobustToolbox/Robust.Shared/Physics/Systems/SharedPhysicsSystem.cs:279,296-299`
   `// TODO PHYSICS Fix Collision Mispredicts`). **No targeted fix exists in Mount's own code** — would
   require either an upstream engine change (extend `ActivateLerp` to smooth corrected-but-already-rendered
   ticks) or a Respiral client-side band-aid (detect large corrections, lerp them out over 1-2 frames
   instead of popping) — the latter risks its own visual artifacts on legitimate large jumps
   (teleports/portals). User decision: leave as-is, this is cosmetic and shared with vanilla Mech.

7. **"Horse rolls in a direction unrelated to my keys after mounting"** — a genuine upstream engine gap
   (relay sources never refresh their own `RelativeRotation` after `SetRelay`, `SharedMoverController.cs`
   `HandleMobMovement`'s relay branch returns before reaching `TryUpdateRelative`) surfaced specifically by
   Respiral's Z-level travel disabling the normal cross-map `RelativeRotation` reset. Fixed with a marked
   upstream edit — full writeup in [[zlevels]] "Z-level travel disables a vanilla `RelativeRotation`
   reset."

8. **"Horse slides backward while standing still, right after mounting" — a SEPARATE bug from #7, user's
   own diagnosis was correct: rider/mount physically collide.** The rider and mount are both ordinary mobs
   on `MobMask`/`MobLayer`; sitting at `Strap.BuckleOffset` (close to/overlapping the mount's own Fixture)
   means the physics engine treats them as two solid bodies shoving each other apart every tick — this
   reads exactly like "gliding"/sliding, easy to mistake for a movement-direction bug (#7) since both
   surface as odd horse motion right when riding starts. Ordinary furniture (chairs) never triggers this
   because furniture isn't on `MobLayer` to begin with, so nobody in this codebase had ever needed to
   solve it before. `BuckleComponent.DontCollide` (`Content.Shared/Buckle/Components/BuckleComponent.cs:35`)
   exists for exactly this, but (a) defaults to `false` and (b) is **never set anywhere in this repo** — a
   genuinely dead field. Worse, it's `[Access(typeof(SharedBuckleSystem))]`-restricted at the class level
   with no per-field override, so `SharedMountSystem` couldn't just do `args.Buckle.Comp.DontCollide = true`
   from outside (`error RA0002: Tried to perform a 'Write' other-type access`) — using it would have
   required an upstream edit to loosen that Access. **Fix avoids the upstream edit entirely:**
   `SharedMountSystem` now subscribes `<RiderComponent, PreventCollideEvent>` directly and cancels the
   collision itself when `args.OtherEntity == ent.Comp.Mount`. This works because `PreventCollideEvent` is
   raised on **both** entities in a contact pair independently (`RobustToolbox/Robust.Shared/Physics/Systems/SharedPhysicsSystem.Contacts.cs:789-799`,
   `RaiseLocalEvent(uid, ...)` then `RaiseLocalEvent(other, ...)`, either one cancelling is enough) — no
   need to also subscribe on `MountableComponent`, and no need to touch `BuckleComponent` at all.

**Why not rewrite Mount around the `Mech` container/pilot-slot model?** Investigated as an alternative
(`Content.Shared/Mech/`) — Mech already uses the exact same `_mover.SetRelay` API Mount uses (via
`SharedMechSystem.cs:134` `SetupUser`), so relaying input isn't the problem needing Mech's architecture.
The actual difference is containerization: Mech's pilot goes into a `ContainerSlot` and becomes
untargetable/invisible (a mech has its own sprite, not the pilot's), with `MechSystem.OnDamageChanged`
manually forwarding a damage fraction to the contained pilot. That model fits a "vehicle you disappear
into," not a horse where the rider should stay **visible, sitting on top** — which Buckle/Strap already
gives for free via Transform parenting. Adopting Mech's container model here would have been a strictly
bigger rewrite to solve a problem (visible rider) it isn't designed for; `Content.Server/_Respiral/Wear/ContainerDamageDistributionSystem.cs`
remains available if a future container-based creature ever needs damage passthrough, but Mount doesn't need it.

**Death: pure `RotationVisuals`, no `dead`/`dead_harness` sprite state, no `DamageStateVisualsComponent`
override needed.** The horse falls over (rotates) on Critical/Dead instead of swapping to a dedicated
dead sprite — see the "Falling over on death/crit" section above for the mechanism. Because rotation
applies to the whole `SpriteComponent` (all layers at once), a saddled horse that dies rotates saddle
and body together for free — this is what let the original 4-state (`alive`/`dead`/`harness`/
`dead_harness`) art get reduced to just 2 states (`alive`/`harness`), and eliminated the need for any
custom C# combining "alive/dead" × "saddled/not" into one appearance value. `- type: RotationVisuals`
needs `defaultRotation: 90` + `horizontalRotation: 90` explicitly — the generic `BaseControllable` mob
root sets both to `0` (rotation disabled, since ordinary animals rely on `DamageStateVisuals` dead-sprite
swap instead), so a Flora mob must explicitly re-enable it, exactly mirroring
`Body/species_base.yml`'s `BaseSpeciesMob` (the humanoid "falls over" prototype).

**"Saddle layer looks crooked while riding/moving" — investigated 2026-08, root cause is BAD ART, not a
rendering bug.** Full mechanical audit (all confirmed dead ends, ruled out in this order):
- `SlotDefinition.Offset` (`Content.Shared/Inventory/InventoryTemplatePrototype.cs:52`) defaults to
  `Vector2.Zero` and the horse's `horse_inventory_template.yml` never sets `offset:` — no stray offset.
- `ClothingComponent.MappedLayer` (`Content.Shared/Clothing/Components/ClothingComponent.cs:25-26`) is
  ONLY consumed by `Content.Client/Clothing/FlippableClothingVisualizerSystem.cs:33` (fold/unfold satchel
  scale sync) — irrelevant to a saddle, not a "better alignment mechanism".
- `ClientClothingSystem.RenderEquipment` (`Content.Client/Clothing/ClientClothingSystem.cs:247-369`): no
  bookmark on the horse's base `Sprite` → `index = _sprite.LayerMapReserve(...)` (line 330) appends the
  `saddle` layer at the END of the layer list → full occlusion of the body layer. This is the documented,
  intended behavior (see above), confirmed correct.
- Per-layer direction/rotation is NOT cached per-layer at insertion time — `Layer.GetLayerDrawMatrix`
  (`RobustToolbox/Robust.Client/.../SpriteComponent.cs:1630-1641`) recomputes the draw matrix from the
  entity's current world rotation **every render call, uniformly for every layer** — a freshly
  `LayerMapReserve`'d layer cannot "lag behind" the body layer's facing by even one frame.
- `PrototypeLayerData.RenderingStrategy` (`RobustToolbox/.../SpriteLayerData.cs:30`) defaults to
  `LayerRenderingStrategy.UseSpriteStrategy` for a freshly-created `Layer`
  (`SpriteComponent.cs:1312`) — same as the body layer — so no divergent per-layer rotation strategy.
  `SpriteComponent.NoRotation` (`noRot`, `SpriteComponent.cs:489-490`) is set `true` on `BaseControllable`
  (`Resources/Prototypes/Entities/Mobs/base.yml:8`) and inherited down the full parent chain to
  `MedievalMobHorse` (nothing overrides it) — applies uniformly to ALL layers of the entity, so even if it
  were off, it'd skew body and saddle identically, not just the saddle.
- No vanilla precedent exists for "clothing layer points back at the wearer's own body RSI for a full
  swap" — searched every non-humanoid `InventoryTemplates/*.yml` (`pet`, `hamster`, `monkey`, `kangaroo`,
  `parrot`, `scurret`, `smartcorgi`); their `whitelist: tags: [PetWearable/HamsterWearable/...]` items
  (`Clothing/Masks/masks.yml`, `Clothing/Eyes/glasses.yml`, `Clothing/Head/hats.yml`, `Clothing/Neck/ties.yml`,
  etc.) are all small vanilla human accessory sprites reused as-is, never a same-RSI full-body composite.
  Respiral's saddle approach is a genuinely novel (but mechanically sound) pattern here.
- **Actual cause, confirmed by opening the PNGs directly**
  (`Resources/Textures/_Respiral/Mobs/Flora/mount.rsi/{alive,harness}.png`, both `directions: 4` per
  `meta.json`): in `alive.png` all 4 directional frames show the horse in a normal standing pose. In
  `harness.png` the two side-profile frames (the ones shown while moving east/west) show the horse's legs
  crossed/bent into an unnatural, twisted pose — visibly different from the equivalent `alive.png` frames.
  This is a source-art defect in the `harness` state's side-view frames, not a rendering/offset/layering
  bug — matches the report exactly ("crooked... while riding/moving", i.e. when the side-profile frames
  are on screen; standing still facing south may look fine). **Fix is re-drawing/re-exporting the
  side-profile (E/W) frames of `harness.png` to match `alive.png`'s leg pose**, not a YAML/C# change.

### Rider vs mount draw depth (whole-entity only, no per-limb occlusion)

User wanted directional layering: south = rider fully behind mount, north/east/west = rider in front
except the far leg tucked behind. **The per-limb version is not possible in this engine** — Robust's draw
depth sorts whole `SpriteComponent`s against each other; it cannot interleave one entity's individual
layers (e.g. a humanoid's `HumanoidVisualLayers.LLeg`) with another entity's layers mid-stack. Confirmed
by vanilla's own code/comments: `Content.Client/Buckle/BuckleSystem.cs:37-84` `OnStrapMoveEvent` does the
same trick for chairs — only a binary "is the strap facing North" check, whole-buckled-entity depth swap
(`_sprite.SetDrawDepth((buckledEntity, buckledSprite), strapSprite.DrawDepth - 1)`) — and its own comment
admits this is a known engine gap: *"Give some of the sprite rotations their own drawdepth... And we
won't ever need to set the draw depth manually. This is quite cursed... I need to finally get around to
reworking sprite rendering for entity layers & direction dependent sorting."*

**Shipped instead: whole-rider depth toggle across all 4 directions** (south=behind, north/east/west=in
front), Mount's own extension of that same vanilla trick:
- `Content.Client/_Respiral/Mount/RiderDepthComponent.cs` — client-only marker, `OriginalDrawDepth: int`,
  presence = "currently overridden."
- `Content.Client/_Respiral/Mount/MountSystem.cs : SharedMountSystem` (the Client half of the abstract
  Shared system, see the ActiveNPC section above for why it must be a separate concrete subclass) — adds
  its own `SubscribeLocalEvent<MountableComponent, MoveEvent>` (free to claim; unlike `StrappedEvent`
  /`UnstrappedEvent` it isn't already subscribed by the Shared base) for direction updates while riding,
  and overrides `OnRiderMounted`/`OnRiderDismounted` (the same hooks the Server half uses for HTN
  sleep/wake) instead of subscribing to `StrappedEvent`/`UnstrappedEvent` directly — **a first attempt
  used a separate standalone `MountVisualsSystem` subscribing straight to `StrappedEvent`/`UnstrappedEvent`
  and crashed the client on login with the exact same "Duplicate Subscriptions" error as the earlier
  ActiveNPC/Server mistake**, since `SharedMountSystem` (Shared, runs on client too) already owns those.
  The hook signatures were changed to `OnRiderMounted(EntityUid mount, EntityUid rider)`/
  `OnRiderDismounted(EntityUid mount, EntityUid rider)` — passing rider explicitly, because by the time
  `OnRiderDismounted` fires `MountableComponent.Rider` has already been nulled in `OnUnstrapped`, so a
  hook that only received `mount` would have no way to look the rider up.
- On mount/move: computes `_xform.GetWorldRotation(mount) + _eye.CurrentEye.Rotation` →
  `.GetCardinalDir()` (same computation vanilla Buckle uses) and sets the rider's draw depth to
  `mountDrawDepth - 1` (South) or `mountDrawDepth + 1` (otherwise); on dismount restores
  `RiderDepthComponent.OriginalDrawDepth` and removes the component.
- The mount's own `- type: Strap` sets `modifyBuckleDrawDepth: false` so vanilla's generic North-only
  trick doesn't also fire and fight over the same rider's draw depth.
- No true "legs behind" illusion — that's left to the mount's own art (wide/tall silhouette covering the
  far leg by simple overlap), same conclusion as the crooked-harness investigation above.
- **General lesson (recurring theme for this whole feature):** any new client- or server-only behavior
  that needs to react to `StrappedEvent`/`UnstrappedEvent` (or any event a Shared system already
  subscribes to) must go through `SharedMountSystem`'s virtual hooks, never a fresh
  `SubscribeLocalEvent<MountableComponent, StrappedEvent>` in a different system — Robust allows exactly
  one subscriber per (component, event) pair, full stop, regardless of which assembly/side is asking.

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
