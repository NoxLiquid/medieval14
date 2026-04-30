# Inventory — Slot System

## How Inventory Slots Work

1. **SlotFlags** (`Content.Shared/Inventory/SlotFlags.cs`) — bitfield enum defining slot types (HEAD, FEET, LEGS, etc.)
2. **InventoryTemplatePrototype** (`Content.Shared/Inventory/InventoryTemplatePrototype.cs`) — C# data definition for `SlotDefinition` (name, slotFlags, uiWindowPos, strippingWindowPos, displayName, stripTime, dependsOn, etc.)
3. **Inventory templates** (YAML) — `Resources/Prototypes/InventoryTemplates/human_inventory_template.yml` defines all slots for humanoids
4. **TemporarySlotMap** (`Content.Client/Clothing/ClientClothingSystem.cs`, ~line 30) — maps slot name to RSI state prefix (e.g. `"shoes"` → `"FEET"`, `"pants"` → `"LEGS"`)
5. **Slot textures** — UI icons at `Resources/Textures/Interface/{Theme}/Slots/{textureName}.png`; each theme (Default, Retro, Ashen, Minimalist, Clockwork, Slimecore, Plasmafire) has its own set

## Adding a New Slot — Checklist

- Add flag to `SlotFlags.cs` if needed (LEGS already exists)
- Add entry in `human_inventory_template.yml` with unique `uiWindowPos` and `strippingWindowPos`
- Add mapping in `TemporarySlotMap` in `ClientClothingSystem.cs`
- Create slot icon PNG in each theme's `Slots/` directory (or reuse existing texture name)
- Clothing items use `equipped-{STATE_PREFIX}` RSI state (e.g. `equipped-LEGS`)

## Current Respiral Additions

- **Pants slot** added: uses `LEGS` flag, `TemporarySlotMap` entry `"pants"` → `"LEGS"`, placed at `uiWindowPos: 0,0` / `strippingWindowPos: 0,3`
- **Sex-specific clothing visuals** in `ClientClothingSystem.OnGetVisuals`: lookup chain is `{slot}-{species}-{sex}` → `{slot}-{species}` → `{slot}-{sex}` → `{slot}` → default RSI

## UI Grid Positions (human template, default group)

| Slot | uiWindowPos | strippingWindowPos |
|---|---|---|
| pants | 0,0 | 0,3 |
| shoes | 1,0 | 1,3 |
| suitstorage | 2,0 | 2,5 |
| jumpsuit | 0,1 | 0,2 |
| outerClothing | 1,1 | 1,2 |
| gloves | 2,1 | 2,2 |
| neck | 0,2 | 0,1 |
| mask | 1,2 | 1,1 |
| ears | 2,2 | 2,0 |
| eyes | 0,3 | 0,0 |
| head | 1,3 | 1,0 |

## Known Gaps

- Pants slot uses `uniform` placeholder texture — needs dedicated `pants.png` icons for all UI themes
- No base pants clothing entity/prototype created yet
