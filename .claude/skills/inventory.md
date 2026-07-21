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
- **Add a layer "bookmark" `- map: ["<slot>"]` in `Resources/Prototypes/Body/species_appearance.yml`** (prototype `BaseSpeciesLayers`) at the desired draw depth — MANDATORY, otherwise this slot's clothing renders on top of everything (see below)
- Create slot icon PNG in each theme's `Slots/` directory (or reuse existing texture name)
- Clothing items use `equipped-{STATE_PREFIX}` RSI state (e.g. `equipped-LEGS`)

## Clothing draw order on the character (CRITICAL)

The draw depth of worn clothing is set by the ORDER of bookmark layers in `Body/species_appearance.yml`
(`BaseSpeciesLayers` → `Sprite.layers`, each `- map: ["<slotName>"]`). Earlier in the list = lower.

`ClientClothingSystem.RenderEquipment`: for a slot it looks for a bookmark layer with key = slot name.
- **bookmark exists** → the clothing layer is inserted right after it (fixed depth);
- **NO bookmark** → `LayerMapReserve` adds the layer at the END of the list → the clothing renders **on top of everything**
  (hands, armor, head). This is a typical bug when adding a new slot.

Example (the fixed `pants` slot bug): without `- map: ["pants"]` the pants rendered over the shirt,
the hands, and `outerClothing`. The bookmark was added **before** `jumpsuit` (between underwear and the body
clothing) → the shirt/tunic renders over the pants, and the pants themselves — under the hands, footwear, and armor.
The position of pants RELATIVE to jumpsuit determines what's over what: `pants` before `jumpsuit` =
shirt over pants; after = pants over shirt. `uiWindowPos`/`TemporarySlotMap` do NOT
affect this — only the order in `species_appearance.yml`.

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

## Bottom hotbar layout (slotGroup)

Slots with a `slotGroup` (NOT `null`) don't go into the equipment doll window but render in the bottom
hotbar. The containers are defined in `Content.Client/.../Hotbar/Widgets/HotbarGui.xaml`:
two `ItemSlotButtonContainer`s — `MainHotbar` and `SecondHotbar` — sit on either side of the hands.

**The button order within a group = the order of slot definitions in the inventory template** (not `uiWindowPos`!).
Rendering: `InventoryUIController.AddSlot` → `container.TryAddButton` → `AddChild` in the iteration order of
`SlotData` (= the slot order in YAML). `uiWindowPos`/`strippingWindowPos` for grouped slots do NOT
affect the hotbar layout (used only by the stripping window and the equipment toggle bar).

Current layout (Respiral):
- `MainHotbar`: `pocket1, pocket2, id, back` — id/PDA and backpack sit right after the pockets.
- `SecondHotbar`: `belt, suitstorage`.

To rearrange a slot in the hotbar — change its `slotGroup` and/or its position among the slot
definitions in `human_inventory_template.yml`, not `uiWindowPos`.

## Known Gaps

- Pants slot uses `uniform` placeholder texture — needs dedicated `pants.png` icons for all UI themes
- Base prototypes and clothing content now exist — see `clothing.md`
