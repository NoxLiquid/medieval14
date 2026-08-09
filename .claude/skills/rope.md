# Rope — tying two entities together

Lets a player tie one entity to another with a rope item: click entity A with the rope, then entity B —
they become tethered (pull each other), a brown rope line is drawn between them, and neither can be freed
until the "Untie rope" context verb is used.

## Files

| Path | Role |
|---|---|
| `Content.Server/_Respiral/Rope/RopeComponent.cs` | On the rope ITEM. **Server-only** (all tie logic is server-authoritative; client just ignores the unregistered `- type: Rope`, same as `GhostRole`). Holds `PendingTarget` + `RopeVisual` (`SpriteSpecifier` datafield). |
| `Content.Shared/_Respiral/Rope/RopedComponent.cs` | On each of the two TIED entities. `[NetworkedComponent]`, `Partner` (networked) + `Rope` (server-only uid of the consumed rope item). Presence blocks stopping the pull. |
| `Content.Shared/_Respiral/Rope/TieRopeDoAfterEvent.cs` | DoAfter event for the second (tie) click; carries the first target as `NetEntity`. |
| `Content.Shared/_Respiral/Rope/SharedRopeSystem.cs` | Shared. Cancels `AttemptStopPullingEvent` for any `RopedComponent` (so the pull can't be released normally). Runs both sides so the client predicts the block. |
| `Content.Server/_Respiral/Rope/RopeSystem.cs` | Server. `AfterInteractEvent` two-click tie, `GetVerbsEvent<Verb>` untie verb, pulling + `JointVisualsComponent` line. |
| `Resources/Prototypes/_Respiral/Entities/Items/Misk/rope.yml` | `MedievalRope` item (cloth). |
| `Resources/Prototypes/_Respiral/Recipe/recipe.yml` + `packs.yml` | `MedievalRopeRecipe` on the `MedievalSewing` (sewing machine) pack, 100 `MedievalCloth`. |
| `Resources/Locale/{en-US,ru-RU}/_Respiral/rope.ftl` | Popup + verb strings. |

## How the tether works — pulling, NOT a raw joint

Mobs are `KinematicController` physics bodies; **a raw distance joint does not drag them** (the solver
ignores forces on kinematic bodies — that's why the grappling hook applies manual impulses). SS14's
`PullingSystem` is what actually drags kinematic mobs (proven by the horse-mount feature, see [[mobs]]).
So the rope tie calls `_pulling.TryStartPull(a, b)` — `a` pulls `b`. It's one-directional in bookkeeping
but physically tethers both (`b` is constrained near `a`; either moving drags the other once taut).

`JointVisualsComponent` (`Content.Shared/Physics/`, rendered by `Content.Client/Physics/JointVisualsOverlay.cs`)
draws the rope line: its overlay redraws the sprite stretched between the owner and `Target` entities'
**live** transforms every frame — exactly a rope that follows two moving entities. This is the same
renderer the grappling hook uses. **NOT `BeamSystem`** — beams (`Content.*/Beam/`) spawn a *fixed*
one-shot beam entity (for tesla/lightning arcs) that does not re-anchor as entities move, so they're wrong
for a persistent rope between two movers. The default `RopeVisual` reuses the grappling gun's `rope` state
(`Objects/Weapons/Guns/Launchers/grappling_gun.rsi`) — a thin rope strip centered in a transparent 32×32
frame, so the stretched line reads as a thin rope. A full 32×32 icon (like `rope.rsi`'s coil `icon`) would
draw as a fat 1-tile band, so it's deliberately NOT used for the line (only for the item sprite). Point
`RopeComponent.RopeVisual` at a thin brown `rope` state added to `rope.rsi` for a custom look.

## Unbreakable until untied — but only the ROPE's own pull

`SharedRopeSystem` cancels `AttemptStopPullingEvent` (namespace `Content.Shared.Pulling.Events`, NOT
`...Movement.Pulling.Events`) — same pattern cuffs use (`SharedCuffableSystem.HandleStopPull`). **Critical
scoping (bug fix):** it must cancel ONLY when the pullable's current `PullableComponent.Puller ==
RopedComponent.Partner`, i.e. only the rope's own pull. A first version cancelled ALL stop attempts on any
roped entity, which meant if a player separately started pulling a tied entity they could never let go
(`args.Cancelled` unconditionally). Checking `Puller == Partner` leaves unrelated player-initiated pulls
releasable. The "untie rope" verb (`GetVerbsEvent<Verb>` on `RopedComponent`) removes `RopedComponent`
from **both** ends FIRST (so the block no longer applies), removes the `JointVisualsComponent`, then calls
`TryStopPull`. Order matters — stopping the pull before removing `RopedComponent` would be self-blocked.

## DoAfter on the tie + rope consumed into a container

Tying is a two-step interaction: first click stores the target (instant); the **second** click starts a
`TieRopeDoAfterEvent` DoAfter (`RopeComponent.TieDelay`, 3s default, `BreakOnMove`/`BreakOnDamage`/`NeedHand`).
The event (`Content.Shared/_Respiral/Rope/TieRopeDoAfterEvent.cs`, `[Serializable, NetSerializable]`,
carries the first target as a `NetEntity` since DoAfterArgs only track user/target/used) is directed at the
rope (`eventTarget: ent.Owner`) so the server `RopeSystem.OnTieDoAfter` handles it. `PendingTarget` is
cleared when the DoAfter STARTS (carried in the event instead), so it can't be re-triggered mid-cast.

On DoAfter success `TryTie` also **consumes the rope into a `ContainerSlot` (`rope_knot`) on entity `a`**
(`_container.EnsureContainer` + `Insert`) — the rope leaves the player's hand and becomes the knot. The
rope entity uid is stored in `RopedComponent.Rope` (non-networked, server-only) on both ends. If the insert
fails, the pull is rolled back and the tie aborts. **Untie** (verb now passes `args.User`) removes the
components, stops the pull, then `_container.TryRemoveFromContainer(rope)` + `_hands.TryPickupAnyHand(user,
rope)` — the rope returns to the untier's hand (or drops at their feet if no free hand). The rope keeps its
`RopeComponent`, so it's reusable after untying.

## Tether length — shortened after tie

Pulling has no length datafield: the pull's distance joint is created with `MaxLength = <distance at
tie-time> + 0.15` (`PullingSystem.cs:563-571`, hardcoded). After `TryStartPull`, `RopeSystem.ShortenTether`
grabs that joint (by `PullableComponent.PullJointId`, cast to `DistanceJoint`) and divides `Length` and
`MaxLength` by `RopeComponent.TetherDistanceDivisor` (default 2.5) — so the rope leash is ~2.5× shorter
than the raw pull distance. Must `Dirty(pullable, jointComp)` after so the client sees the new length
(else it mispredicts). `JointComponent` lives in `Robust.Shared.Physics` (NOT `...Physics.Components`);
`DistanceJoint` in `Robust.Shared.Physics.Dynamics.Joints`.

## Bindable tag gate — at least ONE end must be tagged

Rule (changed): you can tie **anything to a tagged entity** — the tie is allowed if `HasTag(first) ||
HasTag(second)` (at least one has `MedievalRopeBindable`), refused only if NEITHER does (popup
`rope-not-bindable`). Earlier it required BOTH; that blocked e.g. tying a horse (tagged) to a cart
(untagged). The check is NOT per-click (any physics entity is selectable) — it runs once at tie time in
`OnAfterInteract` right before the DoAfter (`TagSystem.HasTag` needs a `ProtoId<TagPrototype>` const,
`[ForbidLiteral]` forbids a bare string). Tag defined in `_Respiral/tags.yml`; on `MedievalMobHorse`. Add
it to anything that should be a valid rope anchor; the other end can then be any physics entity.

## Making arbitrary entities pull — the NeedsHands wall

`TryStartPull` requires the puller to have `PullerComponent` and (if `NeedsHands`, the default) a free
hand. To tie handless entities (animals, crates) the rope sets `NeedsHands = false` on a freshly-added
puller. But `PullerComponent` is `[Access(typeof(PullingSystem))]` and `NeedsHands` had no per-field
override, so writing it from `RopeSystem` failed the `RA0002` analyzer (both `x.NeedsHands = false` AND the
`new PullerComponent { NeedsHands = false }` object-initializer form are blocked). **Fix: one marked
upstream edit** (`Content.Shared/Movement/Pulling/Components/PullerComponent.cs`, `// Respiral-Start/End`)
adding `Access(Other = AccessPermissions.ReadWriteExecute)` to the `NeedsHands` datafield — mirrors the
existing per-field override on `NextThrow` in the same component; `NeedsHands` is a benign config bool.
The rope only relaxes `NeedsHands` on a puller it just added (`if (!hadPuller)`), never changing a real
puller (e.g. a player) that already had the component.

## Known limitations / notes

- Tie logic is server-authoritative (AfterInteract + verb subscribed only in the server `RopeSystem`);
  only the stop-block is shared/predicted. This avoids predicting the pull+joint+component adds.
- The rope item is a reusable tool (not consumed on tie). If consumption/return-on-untie is wanted later,
  add it in `TryTie`/`Untie`.
- `RopedComponent` blocks ALL `AttemptStopPullingEvent`, including engine auto-stops (death, container
  insert). A roped mob that dies stays pulled until untied — cosmetic, the untie verb still cleans up.
- Both entities need a `PhysicsComponent`; `TryTie` checks the second target has one and refuses self-ties.
