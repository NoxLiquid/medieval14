# Combat & Damage System

## Damage Containers

| ID | Supported |
|---|---|
| `MedievalInorganic` | Brute, Burn, Structural |
| `MedievalBiological` | Brute, Burn, Toxin, Airloss, Genetic |
| `MedievalClothing` | Brute, Burn |

## Damage Modifier Sets (structures)

| ID | Blunt | Slash | Pierce | Heat | Structural |
|---|---|---|---|---|---|
| `MedievalWood` | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 |
| `MedievalStone` | 0.7 | 0.7 | 0.7 | 0.3 | 1.0 |
| `MedievalIron` | 0.55 | 0.55 | 0.75 | 0.5 | 1.0 |
| `MedievalSteel` | 0.45 | 0.45 | 0.65 | 0.2 | 1.0 |
| `MedievalMithril` | 0.35 | 0.35 | 0.50 | 0.2 | 1.0 |
| `MedievalAdamantine` | 0.15 | 0.15 | 0.30 | 0.1 | 1.0 |
| `MedievalGold` | 0.7 | 0.7 | 0.8 | 0.4 | 1.0 |
| `MedievalSilver` | 0.8 | 0.8 | 0.9 | 0.5 | 1.0 |

Note: `DamageTrigger` checks accumulated damage **after** modifiers.

## Wieldable In-Hand Visuals

- `GenericVisualizer` for `WieldableVisuals.Wielded` was removed from base weapons — it required a non-existent `wielded` RSI state and a missing `Appearance` component
- Wielded in-hand display is handled via `HeldPrefix` ("wielded") set by `SharedWieldableSystem`
- `ItemSystem.OnGetVisuals` (Respiral modification) now applies `HeldPrefix` to explicit `inhandVisuals` layers: if `wielded-{state}` exists in the RSI, it replaces the original state
- This allows weapons with explicit `inhandVisuals` (for per-material color tinting) to also support wielded sprites
- RSIs for two-handed weapons need both `inhand-left/right` and `wielded-inhand-left/right` states
