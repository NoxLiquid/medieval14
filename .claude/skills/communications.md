# Communications Console (Medieval)

## Connecting magic ball

**Entity:** `MedievalConnectingMagicBall`
**Detail:** `MedievalConnectingMagicBallDetail`

A medieval replacement for the communications console. Allows broadcasting announcements to the station without electricity.

### Key decisions

- Inherits `MedievalConstructibleMachine` + `MedievalBaseMachine` (standard `MedievalMachine` graph)
- Works **without electricity** (no `ApcPowerReceiver`, `Computer`, `PowerState`)
- `canShuttle: false` — can't call a shuttle (medieval)
- No `AccessReader` — the ball is available to everyone
- Sprite: `_Respiral/Structures/Machines/connecting_magic_ball.rsi` (animation `running`)
- Glow: purple `#7b68ee`

### Crafting

Uses the standard `MedievalMachine` graph:
1. Place `MedievalUnfinishedMachineFrame` (stone in the frame composition)
2. Add `MedievalManaCrystal` (magic crystal)
3. Insert `MedievalConnectingMagicBallDetail` (detail)
4. Screw (Screwing)

### Detail

`MedievalConnectingMagicBallDetail` — inherits `MedievalBaseMachineDetail`, contains `MachineBoard` with `prototype: MedievalConnectingMagicBall`. Composition: `MedievalProcessedStone: 300`, `MedievalRawManaCrystal: 200`.

### Files

| File | Contents |
|---|---|
| `Entities/Structures/Machine/connecting_magic_ball.yml` | Entity prototype |
| `Entities/Objects/Parts/production.yml` | Detail (MedievalConnectingMagicBallDetail) |
