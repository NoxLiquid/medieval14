# In-hand icon fallback + per-direction body layering

Client-only feature (`Content.Client/_Respiral/Inhand/`). No upstream edits, no YAML — automatic
for every item.

## What it does
If a held item has NO in-hand sprite (no explicit `inhandVisuals` and no `inhand-{left/right}` RSI
state — vanilla would show nothing), it synthesises one from the item's own icon at **`IconScale` (0.5)**.
The item's whole layered icon is copied (each visible `ISpriteLayer`: state + rsi + color + scale*IconScale),
so multi-layer tinted icons (e.g. mantles) reproduce correctly in hand.

## Offset toward the hand
The icon is shifted to the hand via `LayerSetOffset` per hand + facing (constants `HandOffsetSide` 0.28,
`HandOffsetForward` 0.30, `HandOffsetY` -0.05, world units). Set in `ApplyOrder` (runs with visibility on
direction change), NOT baked at build time — the hand's screen position changes with facing:
- S: Left → +X, Right → -X;  N: mirrored;  E: +X (forward);  W: -X.
Tune the three constants + `IconScale` to taste.

## How the layering works
The engine draws sprite layers by index (global, not per-direction), so front/back vs the body must be
switched at runtime when the mob turns. Two copies of the icon are placed:
- **over** layers: added via `GetInhandVisualsEvent` → `HandsSystem` appends them on top.
- **under** layers: inserted at sprite index 0 (behind the whole mob) in `HeldVisualsUpdatedEvent` via
  `SpriteSystem.AddLayer(data, 0)` (engine shifts indices + remaps keys; `MapKeys` auto-registers).
  Their keys are added to the event's `RevealedLayers` so `HandsSystem` cleans them up on refresh.

Per facing + hand, exactly one set is shown (`LayerSetVisible`); the other hidden. Facing =
`GetWorldRotation(holder).GetCardinalDir()` (the mover rotates the mob's transform to face, unless
`NoRotateOnMove`). Updated on `MoveEvent` (turning), short-circuited by `LastDirection`.

Rule — `ItemOnTop(hand, dir)` (true = item above body):
- Left hand:  S=on-top, N=under, E=under, W=on-top
- Right hand: S=on-top, N=under, E=on-top, W=under

## Files
- `MedievalIconInhandComponent.cs` — on the HOLDER; per-`HandLocation` fallback (over/under keys + under
  layer data) + `LastDirection`.
- `MedievalIconInhandSystem.cs` — public `BuildFallback(uid, args)` builds the over layers + records the
  component; `HeldVisualsUpdatedEvent` creates under layers + prunes stale hands; `MoveEvent` re-applies order.
- `Content.Client/Items/Systems/ItemSystem.cs` — **upstream edit (Respiral-Start/End)**: `OnGetVisuals`
  calls `_iconInhand.BuildFallback` when no in-hand layers resolved.

## GOTCHA: one directed subscription per (component, event)
Robust throws `Duplicate Subscriptions for comp=…, event=…` if two systems both
`SubscribeLocalEvent<TComp, TEvent>`. `GetInhandVisualsEvent` on `ItemComponent` is already owned by
upstream `ItemSystem`, so the fallback CANNOT be a second subscription — it's invoked from inside
`ItemSystem.OnGetVisuals` instead (via a `[Dependency]` on `MedievalIconInhandSystem`, Respiral markers).
`HeldVisualsUpdatedEvent`/`MoveEvent` had no existing subscriber, so those stay in the Respiral system.

## Caveats / not yet verified in-game
- Facing uses world-rotation cardinal; on rotated maps (non-zero eye rotation) it may be off by the eye
  offset. Not compensated.
- "Under body" = index 0 (behind the entire mob sprite), i.e. behind body AND clothing, not just torso.
- Needs visual confirmation in a running client (build compiles; rendering not runtime-tested here).
