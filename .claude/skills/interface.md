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

### Untouched / for the future
- `BorgHealth/BorgCrit/BorgDead` — borg health, no custom sprites, left upstream.
- `Debug1–6` — debug alerts, reference `human_alive.rsi`, not touched.
- `mana.rsi` (mana0–9) — used by the `Mana` alert (see [[mana]]). Prototype in
  `Resources/Prototypes/_Respiral/Alerts/mana.yml`, the `Mana` category added to `BaseAlertOrder`.
- `empty.rsi` — present in the textures, no vanilla equivalent, not used yet.

### Validation
RSIs require `license`/`copyright` in meta.json (otherwise RSI validation fails) — all new RSIs have them filled.
