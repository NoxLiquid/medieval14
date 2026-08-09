# Hitch — attaching a cart to a horse

Lets a draft animal pull a cart that follows behind and turns to face the horse's movement direction.
Attach via a context verb (DoAfter), detach via a context verb.

## Files

| Path | Role |
|---|---|
| `Content.Server/_Respiral/Hitch/HitchableComponent.cs` | Marker on the draft animal (horse). Server-only. |
| `Content.Server/_Respiral/Hitch/HitchComponent.cs` | On the cart. Server-only. `Horse` (null = not hitched), `SearchRange`, `AttachDelay`. |
| `Content.Server/_Respiral/Hitch/HitchSystem.cs` | Verbs, DoAfter, pull, and the rotation Update loop. |
| `Content.Shared/_Respiral/Hitch/HitchDoAfterEvent.cs` | `SimpleDoAfterEvent` for the attach. |
| `Resources/Prototypes/_Respiral/Entities/Structures/cart.yml` | `MedievalCart` gets `- type: Hitch`, `noRot` removed, physics `fixedRotation: true`. |
| `Resources/Prototypes/_Respiral/Entities/Mobs/Flora/Town/horse.yml` | `MedievalMobHorse` gets `- type: Hitchable`. |
| `Resources/Locale/{en-US,ru-RU}/_Respiral/hitch.ftl` | Verb + popup strings. |

## The two hard parts and how they're solved

**Follow without overlap = pull.** The cart is a `BaseStructureDynamic` (Dynamic body), so the vanilla
`PullingSystem` drags it (unlike kinematic mobs — a raw joint wouldn't, but a dynamic cart is dragged
fine). `TryHitch` does `_pulling.TryStartPull(horse, cart)` (horse = puller). The horse gets a
hands-free `PullerComponent` (`NeedsHands = false`, only relaxed if we added it — relies on the marked
upstream edit to `PullerComponent.NeedsHands`, see [[rope]]). The pull's distance joint keeps the cart
behind the horse and `CollideConnected = false` prevents them clipping. The player can't stop this pull
via the normal "stop pulling" verb (that verb only shows for the *puller*, which is the horse, not the
player), so no stop-block is needed — only the detach verb ends it.

**Rotate to face movement direction.** Pull does NOT rotate the pulled body (it sets `FixedRotationOnPull`),
and the cart had `noRot: true`. The lever: **the horse's own Transform rotates to face its movement
direction** — mobs lack `NoRotateOnMoveComponent`, so `SharedMoverController.cs:344` calls
`SetLocalRotation` toward the wish-dir. So `HitchSystem.Update` each tick does
`_transform.SetWorldRotation(cart, _transform.GetWorldRotation(horse))` — the cart copies the horse's
facing. For this to show, the cart's `noRot` was removed from BOTH `Transform` and `Sprite` (Transform
`noRot` forces world rotation to 0, so `SetWorldRotation` would no-op; Sprite `noRot` locks the directional
frame). `Physics.fixedRotation: true` keeps physics from spinning the now-rotatable cart on bumps while
still letting the manual `SetWorldRotation` apply. The cart.rsi `cart`/`cart_empty` states are already
`directions: 4`, so the copied rotation picks the right directional frame.

Rotation is server-only (networked via the transform, client interpolates) — deliberately NOT predicted,
to avoid the prediction/rollback snapping that bit the mount feature ([[mobs]] point 6).

## Interaction flow

- Attach: **drag the horse entity onto the cart** (drag-drop), NOT a verb. The horse is draggable for free
  because it has a `BodyComponent` (`BodySystem` answers `CanDragEvent`); no `Draggable` marker needed.
  **CRITICAL: the drag-drop VALIDATION must run client-side.** The client's `DragDropSystem.GetDropValidity`
  (`Content.Client/Interaction/DragDropSystem.cs:506-521`) decides whether to even SEND the drop request by
  raising `CanDropTargetEvent` **on the client** — if no client-side handler sets `CanDrop`, the client
  rejects the drop and never contacts the server. So:
  - `HitchComponent` and `HitchableComponent` are in **Content.Shared** (not server-only) so the client
    has them — `HitchComponent` is `[NetworkedComponent]` with `Horse` an `[AutoNetworkedField]` (client
    needs `Horse == null` for the check); `HitchableComponent` is a plain shared marker (client gets it from
    the horse prototype). Server code that flips `Horse` must `Dirty` the component.
  - The `CanDropTargetEvent` handler lives in a **shared** `SharedHitchSystem` (runs on both sides) —
    `CanDrop = Horse == null && HasComp<HitchableComponent>(dragged)`.
  - Only the **action** (`DragDropTargetEvent`, raised server-side by `SharedDragDropSystem` after the
    networked request) stays in the server `HitchSystem` (starts the `AttachDelay` (2s) DoAfter → `TryHitch`).
  First attempt put BOTH the CanDrop check AND the components server-only → client never validated → drag
  did nothing. General lesson: any drag-drop where the client must show it as valid needs the target's
  `CanDropTargetEvent` handler + the components it inspects to be shared/networked, not server-only.
  (Note `SharedDragDropSystem.OnDragDropRequestEvent` raises `DragDropDraggedEvent` on the dragged FIRST;
  if that's handled it returns before the target — the horse's `StrippableComponent` only handles it when
  `Target == User`, so dragging onto a cart falls through to the target correctly.)
- Detach: context verb **"Отцепить повозку"** on the cart → `TryStopPull` + clears `Horse`. (Detach stayed
  a verb by explicit request; only attach moved to drag-drop.)
- `PullStoppedMessage` on the cart clears `Horse` if the pull ends for any other reason (cart deleted,
  teleport, etc.), keeping state consistent.

## Cart placement

`MedievalCart` overrides `placement: mode: PlaceFree` (free placement, no grid snap). It inherits
`SnapgridCenter` from `BaseStructureDynamic`, but that placement mode crashes the client's placement
overlay across map boundaries on z-level maps (`SnapgridCenter.Render` → `MapToGrid` throws
`Grid N is on map X, but coords are on map Y`). A free-moving cart doesn't need grid snapping, so
`PlaceFree` is both semantically right and sidesteps the crash.

## Notes / possible tuning

- One horse pulls one cart (a puller pulls one thing; `TryStartPull` fails on a second cart).
- If the cart+horse pull ever conflicts with the rider's buckle joint-relay (rider→horse relay + horse→cart
  pull), watch for the `SharedJointSystem` self-joint assert ([[mobs]] / [[rope]]) — not observed, because
  the horse is a joint-relay *target* (its `JointComponent.Relay` stays null), but keep it in mind.
- Rotation copies the horse's facing exactly (cart aligned with horse). If a trailing/swinging feel is
  wanted instead, switch to a distance joint anchored at the cart's front (physics trailer) — heavier
  tuning, was the rejected alternative.
