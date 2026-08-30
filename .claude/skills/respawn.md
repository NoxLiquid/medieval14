# Ghost respawn timer + button

Death→ghost starts a **15-minute** respawn timer; a button in the ghost-HUD shows the countdown (**red**),
turns **green** when ready, and on click rejoins the player as a new character. Timer **resets** when the player
returns to a living (`MobState.Alive`) body. **Zero upstream edits** — the button is a self-added Respiral widget,
not a modification of upstream `GhostGui`.

## Files
- **Shared** `Content.Shared/_Respiral/Respawn/`:
  - `MedievalGhostRespawnComponent` (on the ghost; `NetworkedComponent, AutoGenerateComponentState`):
    `[AutoNetworkedField] TimeSpan RespawnAt` — when respawn becomes available. Networked so the client renders
    the countdown.
  - `MedievalGhostRespawnRequestEvent` (`[Serializable, NetSerializable] : EntityEventArgs`) — client→server
    respawn request (mirrors upstream `GhostReturnToBodyRequest`).
- **Server** `Content.Server/_Respiral/Respawn/MedievalRespawnSystem.cs`: `Dictionary<NetUserId, TimeSpan>
  _respawnAt`.
  - `GhostComponent + PlayerAttachedEvent` → start timer if absent (`CurTime + 15min`), `EnsureComp` the cache on
    the ghost + `Dirty`.
  - `MobStateComponent + PlayerAttachedEvent` (Alive) and broadcast `MobStateChangedEvent` (→Alive on a player) →
    `_respawnAt.Remove(user)` (the reset). `RoundRestartCleanupEvent` → clear.
  - `SubscribeNetworkEvent<MedievalGhostRespawnRequestEvent>`: require `CurTime >= _respawnAt[user]`, station via
    `StationSystem.GetStations().FirstOrNull()`, then `GameTicker.MakeJoinGame(session, station, silent: true)`
    (the reused upstream respawn primitive — see `Content.Server/GameTicking/Rules/RespawnRuleSystem.cs`). Leaves
    the corpse.
- **Client** `Content.Client/_Respiral/Respawn/`:
  - `MedievalRespawnWidget : UIWidget` — self-positioned top-center (below the ghost buttons). **The widget stays
    `Visible` (else `FrameUpdate` is skipped — `Control.DoFrameUpdateRecursive` returns early on `!Visible`); it
    toggles only the inner `Button`.** `FrameUpdate` reads the local entity's `MedievalGhostRespawnComponent`,
    shows `mm:ss` red / disabled while counting, "Респавн" green / enabled when ready; press →
    `IEntityNetworkManager.SendSystemNetworkMessage(new MedievalGhostRespawnRequestEvent())`. Red/green via
    `Button.ModulateSelfOverride` (no stylesheet-class dependency).
  - `MedievalRespawnUiController : UIController` — on `GameplayStateLoadController.OnScreenLoad`,
    `UIManager.ActiveScreen?.GetOrAddWidget<MedievalRespawnWidget>()` (how widgets get onto the screen without
    editing the screen XAML).
- **Locale** `Locale/ru-RU/_Respiral/respawn.ftl`: `medieval-respawn-button-waiting` (`{ $time }`),
  `medieval-respawn-button-ready`.

## Gotchas learned
- `MeleeWeapon`-style required fields don't apply here, but note: invisible controls get **no** `FrameUpdate`
  (keep the widget visible, toggle children).
- `MedievalGhostRespawnComponent` is auto-networked; server sets `RespawnAt` + `Dirty`, client reads it live.
- Delay is a `static readonly TimeSpan RespawnDelay = 15min` const in the server system — could become a CVar.

## Verify
`dotnet build` (Server+Client) — **must close the running game first** (otherwise MSB3027/MSB3021 file-lock, not
compile errors). In-client: die → ghost → red countdown from 15:00 → (lower the const to test) green → click →
rejoin as new character. Reset: die→ghost→revive into a living body→die→timer restarts from 15:00.
