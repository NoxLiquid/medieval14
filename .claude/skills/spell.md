# Spell — spells and elements

The foundation of magic: spells as Action entities, natural elements as first-class, data-driven forms.
A Respiral theme. The umbrella folder for magic systems — `_Respiral/Magic/` (next to [[mana]] in `Magic/Mana`).

> **STATUS (2026-06): the element abstraction below is the DESIGN, not what's built.** Reality:
> - `SpellComponent` has ONLY `ManaCost` (no `Element` field).
> - There is NO shared `ProjMagicBall`. Spells reference **per-element projectiles**
>   `MedievalProjFireBall` / `MedievalProjWaterBall` (`projectiles.yml`) directly in their
>   `WorldTargetAction.event.prototype`. Each bakes its own sprite state + `Projectile.damage`
>   (Heat 20 / Cold 20) and carries an inert `ElementComponent`.
> - `ProjectileSpellSystem` just `Spawn(args.Prototype)` + `ShootProjectile` — no runtime element application.
> - `ElementSystem` is an empty stub (no `ApplyElement`); `ElementComponent` is **server-only, NOT networked**
>   and currently read by nobody → no element-based tinting/damage yet.
>
> So today a new spell variant = a new per-element projectile prototype + a spell prototype pointing at it.
> The single-ball + first-class-element design is aspirational (TODO). Don't reference `ProjMagicBall` or
> `SpellComponent.Element` — they don't exist.

## Main principle (architecture)

- **A spell = an Action entity** (engine `ActionComponent` + targeting). It gives for free: cooldown,
  icon, target selection, and a **cast sound** (`Action.Sound`, played predictively by the engine).
- **Behavior is dispatched by the action-event TYPE, NOT by a per-spell component.** One system
  per "form" (projectile/beam/nova). This is the upstream idiom (`SharedMagicSystem`), mirrored in `_Respiral`.
- **Do NOT make a component per spell** and **do NOT use interfaces for dispatch** — Robust
  doesn't query components by interface. Each castable spell = a small YAML prototype.
- **Two orthogonal axes:** *form/delivery* (code, one system per form) × *element* (data).
  A new element = 1 `ElementPrototype` + 1 mini-prototype spell per form. No new C#.

## Files

| File | Role |
|---|---|
| `Content.Shared/_Respiral/Magic/Element/ElementPrototype.cs` | `[Prototype]` element: `Name`, `Color`, `Damage` (DamageSpecifier). First-class, groundwork for reactions |
| `Content.Shared/_Respiral/Magic/Element/ElementComponent.cs` | Networked: carries `ProtoId<ElementPrototype>?` on the projectile (the client reads it to tint) |
| `Content.Shared/_Respiral/Magic/Spell/SpellComponent.cs` | On the action entity (NOT networked): `ManaCost`, `Element`. Read only by the server |
| `Content.Shared/_Respiral/Magic/Spell/ProjectileSpellEvent.cs` | `WorldTargetActionEvent` with `Prototype` (projectile). The "ball" form |
| `Content.Shared/_Respiral/Magic/Spell/RevealTrueNameSpellEvent.cs` | `EntityTargetActionEvent` with `Delay` (TimeSpan). The "ritual on target" form |
| `Content.Shared/_Respiral/Magic/Spell/RevealTrueNameDoAfterEvent.cs` | `SimpleDoAfterEvent` (NetSerializable). Concentration completion |
| `Content.Server/_Respiral/Magic/Spell/SpellSystem.cs` | `TryCast(action, performer)`: mana check/spend ([[mana]]), popup. The shared core of all forms |
| `Content.Server/_Respiral/Magic/Spell/ProjectileSpellSystem.cs` | `ProjectileSpellEvent` handler: spawn the projectile, stamp the element, `ShootProjectile`. Server-only |
| `Content.Server/_Respiral/Magic/Spell/RevealTrueNameSpellSystem.cs` | `RevealTrueNameSpellEvent` + DoAfter handler: reveals the target's true name into the caster's `KnownFaces` ([[identity]]). Server-only |
| `Content.Server/_Respiral/Magic/Element/ElementSystem.cs` | `ApplyElement(uid, id)`: sets `ElementComponent` + transfers the element's damage onto `ProjectileComponent` |
| `Content.Client/_Respiral/Magic/Element/ElementVisualsSystem.cs` | Tints the sprite to `ElementPrototype.Color` (on ComponentStartup + AfterAutoHandleState) |
| `Resources/Prototypes/_Respiral/Magic/{elements,projectiles,spells}.yml` | Fire/Water; the shared `ProjMagicBall`; `ActionBaseBallSpell` + Fireball/Waterball |
| `Resources/Locale/{en-US,ru-RU}/_Respiral/magic/spells.ftl` | spell/element names and descriptions, `spell-not-enough-mana` |

## Cast flow (ball)

1. The player presses the action → the engine predictively plays `Action.Sound` and sets the cooldown, raises
   `ProjectileSpellEvent` (Performer, Target, Action).
2. `ProjectileSpellSystem` (server-only) catches the event → `SpellSystem.TryCast` (spend mana; if
   not enough — popup, exit).
3. Spawns the shared `ProjMagicBall`, `ElementSystem.ApplyElement` sets `ElementComponent` (color on
   the client) and overwrites the projectile's damage with the element's damage → `ShootProjectile` toward the target.

## The "ritual on target" form (revealing the true name)

The spell `ActionSpellRevealTrueName` (`EntityTargetAction`, target — an entity with `Identity`):
1. The player selects a person → the engine raises `RevealTrueNameSpellEvent` (Performer, Target, Delay).
2. `RevealTrueNameSpellSystem.OnRevealSpell`: gates — target ≠ caster, the target has `IdentityComponent`,
   the true name isn't known yet (otherwise popup, don't touch mana) → `SpellSystem.TryCast` (spend mana
   at the start) → `DoAfterArgs` with `eventTarget = target` (anchored on the target's `IdentityComponent`).
3. On completion `RevealTrueNameDoAfterEvent` (raised on the target) → `MedievalIdentitySystem.RevealTrueName(performer, target)`
   writes the true name into the caster's `KnownFaces`. After that `Identity.Name(target, viewer=caster)` returns
   it over the anonymous/passport name (see [[identity]]).

- **Access to `KnownFaces` — only through `MedievalIdentitySystem`** (it owns the data: privacy
  via `GetStateAttempt`, the examine verb, "present to one"). The spell system does NOT touch the component
  directly, it calls `RevealTrueName(viewer, target)` (returns the name for the popup) and `KnowsTrueName(viewer, target)`
  (the "already known" gate). The shared helper `EnsureKnownFace` DRYs note creation (passport + magic).
- **The DoAfter anchor — the target's `IdentityComponent`**, not a component of the caster (the caster might lack a
  stable component; the target by definition has `Identity`, otherwise there's nothing to reveal).
- **The true name** = `Name(target)` (`MetaData.EntityName`) — the entity's real name, NOT the
  identity-slot name (the engine substitutes a separate entity) and NOT the passport-shown name.
- **Mana is spent at the start of the cast**, not on DoAfter completion — if concentration is broken, the mana
  is already spent (the same compromise as with cooldown/sound; see below).
- `KnownFacesComponent` is owner-only (state only to the owner) → `Dirty` inside `RevealTrueName`
  syncs the note to the caster's client; other clients don't see it.

## Key decisions / nuances

- **`ProjMagicBall` — ONE shared projectile.** The color (client visualizer) and damage type (server) are set by the
  element at runtime. We do NOT proliferate per-element projectiles. Sprite — `magic.rsi` state `spell` (gray, tinted).
- **`ElementComponent` networked** — this is the correct network case: the client reads the element to tint the
  sprite. (cf. [[feedback-network-logic-necessity]] — network where the client actually reads the field.)
- **`SpellComponent` NOT networked** — the values from the prototype exist on both sides, read only by the server;
  but the component is in Shared so the action prototype loads on the client too (otherwise "unknown component").
- **Known limitation:** the action's cooldown/sound are predicted BEFORE the server's mana check, so on
  insufficient mana they don't roll back. A full pre-cast gate requires replicated mana (deferred
  deliberately, see [[mana]]).
- The C# code growth is only for a new *form mechanic* (beam/nova), not for a new element/spell.

## Not done / for the future

- **Element interactions (reactions).** Hook: `ElementComponent` on the target/tile + a future
  `ElementReactionPrototype` (matrix of pairs → effect). The logic will go into `ElementSystem`.
- New forms: `BeamSpellEvent`/`NovaSpellEvent`/`TouchSpellEvent` + their systems.
- The spells aren't given to anyone — give an action to a player/item (`ActionSpellFireball`/`...Waterball`).
- Own sounds/icons for spells (currently upstream placeholders).
- Prediction of "is there enough mana" (would require making mana networked — not needed yet).
