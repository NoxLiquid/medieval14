# Mana — the mana system

The foundation of magical energy (mana): pool, spending, timer-based regeneration, an alert indicator.
A Respiral theme (life cycles/magic). A base implementation, not tied to spells/mobs.

## Files

| File | Role |
|---|---|
| `Content.Server/_Respiral/Magic/Mana/Components/ManaComponent.cs` | Component (server-only, NOT networked): `Mana`, `MaxMana`, `RegenAmount`, `RegenWindow`, `ManaAlert` + `ManaChangedEvent` |
| `Content.Server/_Respiral/Magic/Mana/ManaSystem.cs` | All logic: API (`HasMana`/`TryUseMana`/`ChangeMana`/`SetMana`), `UpdateManaAlert`, regeneration in `Update` via `TimedWindowSystem`, init on `MapInit` |
| `Resources/Prototypes/_Respiral/Alerts/mana.yml` | `alertCategory: Mana` + `alert: Mana` (mana.rsi, mana0–9, maxSeverity 9) |
| `Resources/Locale/{en-US,ru-RU}/_Respiral/alerts/mana.ftl` | `alerts-mana-name`, `alerts-mana-desc` |

## Key decisions

- **Server-authoritative, no network plumbing.** Everything (component + system) lives on the server;
  `ManaComponent` is NOT a `NetworkedComponent`. The client sees mana ONLY through the alert icon, which is sent
  to the client via the networked `AlertsComponent` (driven by `ShowAlert`), not via component state.
  `AutoNetworkedField`/`NetworkedComponent` aren't needed here — the client never reads the numeric value,
  there's no spend prediction. Add network plumbing only when a client consumer appears
  (a numeric bar / predicted cast). See the memory rule about redundant network logic.
- **Regeneration via TimedWindow** (`Content.Shared/_Respiral/TimeWindow`): `RegenWindow.Remaining`
  stores an absolute deadline (`CurTime + duration`). In `Update`: if `IsExpired` → `Reset` the window, and
  if mana isn't full → `ChangeMana(+RegenAmount)`. Min==Max ⇒ a fixed interval.
- **Window initialization — on `MapInitEvent`, NOT `ComponentStartup`.** `RegenWindow` is a `DataField`;
  mutating it (`Reset`) on `ComponentStartup` breaks the test that the spawned entity matches the
  prototype (the test doesn't call MapInit). See [[testing]]. This is the general rule for mutating DataFields.
- **Alert severity** is computed in `UpdateManaAlert`: `ratio = Mana/MaxMana`, then
  `Lerp(minSeverity, maxSeverity, ratio)` → rounded into [0;9]. min/max come from the alert prototype
  (`AlertsSystem.GetMin/MaxSeverity`), not hardcoded. Same technique as health in `MobThresholdSystem`.
- **The `Mana` category** was added to `BaseAlertOrder` (upstream `alerts.yml`, after Stamina,
  comment `# Respiral`). See [[interface]].

## Not done / for the future

- `ManaComponent` is NOT attached to anyone — add it to a mage/race prototype via YAML:
  `- type: Mana` (+ optional `maxMana`, `regenAmount`, `regenWindow: {min: ..., max: ...}`).
- No mana spending by spells — API consumer: `_mana.TryUseMana(uid, cost)` before a cast.
- No client-side spend prediction (for a responsive cast UI, later make a client subclass of
  `SharedManaSystem` and predict `TryUseMana`).
- `empty.rsi` in the alert textures is not used yet (see [[interface]]).
