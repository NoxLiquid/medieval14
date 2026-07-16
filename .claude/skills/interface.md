# Interface — UI/HUD

Customization of the Respiral user interface (alerts, HUD elements).

## Alerts

Alert prototypes are upstream (`Resources/Prototypes/Alerts/alerts.yml`), the IDs are fixed.
An icon can be overridden **only by editing the upstream file** (you can't duplicate `type: alert` with the same
`id` — prototype conflict). Edits are marked with a `# Respiral - ...` comment.

Custom sprites live in `Resources/Textures/_Respiral/Interface/Alerts/` (ported from CrystallEdge/CrystallPunk14).

### Current replacements (alerts.yml)

| Alert ID | New RSI | States | maxSeverity |
|---|---|---|---|
| `HumanHealth` | `_Respiral/.../alive.rsi` | health0–9 | 9 (was 4, expanded to 10 gradations) |
| `HumanCrit` | `_Respiral/.../critical.rsi` | critical | — |
| `HumanDead` | `_Respiral/.../dead.rsi` | dead | — |
| `Bleed` | `_Respiral/.../bleed.rsi` | bleed0–10 | 10 |
| `Stamina` | `_Respiral/.../stamina.rsi` | stamina0–6 | 6 |

### How health severity works
`MobThresholdSystem.UpdateAlerts` (Content.Shared/Mobs/Systems/MobThresholdSystem.cs:413) computes
severity via `MathHelper.Lerp(minSeverity, maxSeverity, percentage)`. So increasing
`maxSeverity` + adding icon frames automatically gives a smoother gradation (10 levels for health).

> **⚠️ Health severity axis is INVERTED vs mana/stamina.** For health `percentage = damage / threshold`
> (`TryGetPercentageForState`), so **full health → severity 0**, near-crit → severity 9. Mana/stamina use
> `ratio = value / max` (full → max severity). Therefore the health art must run the OPPOSITE way:
> `health0` = FULL orb (shown at full HP), `health9` = empty orb (near death). The CrystallEdge art shipped
> reversed (health0 empty … health9 full, matching mana/stamina) → at full HP you saw an empty orb.
> **Fix:** swapped the PNG files in `alive.rsi` pairwise (`health{i}` ↔ `health{9-i}`); meta.json state names
> and the `alerts.yml` icon list are unchanged (severity still indexes `health{severity}`). If health art is
> ever re-imported, remember to reverse it.

### Alert icon overlap (48×48 sprites)
`AlertControl` (`Content.Client/.../Alerts/Controls/AlertControl.cs`) draws each alert via a `SpriteView`
with `Scale=2`, `Stretch=None`. Upstream alert icons are 32×32 → 64px drawn = fits the `MaxSize`.
Respiral icons are **48×48 → 96px drawn**, which overflowed the old `MaxSize=64`: the sprite is drawn
centered at its full 96px and bled 16px past the reserved cell, overlapping neighbouring alerts in the
vertical stack. **Fix:** `MaxSize` bumped 64→**96** (marked `// Respiral`). The control now reserves the
real drawn size, so 48px icons no longer overlap; 32px upstream icons (64px < 96) are unaffected
(their `MeasureOverride` size stays 64). The `_cooldownGraphic` MaxSize is left at 64 (these three alerts
have no cooldown).

### Alert stack right margin
The whole alert stack sits in a `PanelContainer` (`HorizontalAlignment=Right`) inside `AlertsUI.xaml`.
A small right `Margin="0 0 8 0"` (marked `<!-- Respiral -->`) offsets the stack from the screen edge.
Adjust the third value to change the gap; the individual `AlertControl` is `HAlignment.Left` and shouldn't
be used for this.

### Untouched / for the future
- `BorgHealth/BorgCrit/BorgDead` — borg health, no custom sprites, left upstream.
- `Debug1–6` — debug alerts, reference `human_alive.rsi`, not touched.
- `mana.rsi` (mana0–9) — used by the `Mana` alert (see [[mana]]). Prototype in
  `Resources/Prototypes/_Respiral/Alerts/mana.yml`, the `Mana` category added to `BaseAlertOrder`.
- `empty.rsi` — present in the textures, no vanilla equivalent, not used yet.

### Validation
RSIs require `license`/`copyright` in meta.json (otherwise RSI validation fails) — all new RSIs have them filled.
