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

## Non-humanoid wearable slots (e.g. horse saddle) — reuse a SlotFlags bit + blacklist it on human

`SlotFlags` (`Content.Shared/Inventory/SlotFlags.cs`) is a hardcoded `[Flags]` enum with every bit
already claimed by the human template (`HEAD, EYES, EARS, MASK, OUTERCLOTHING, INNERCLOTHING, NECK,
BACK, BELT, GLOVES, IDCARD, POCKET, LEGS, FEET, SUITSTORAGE`) — there is no "free" flag, and it's C#
code to add a new one. The vanilla answer (already used by `pet_inventory_template.yml`,
`hamster_inventory_template.yml`, `monkey_inventory_template.yml`, etc.) is to give a non-humanoid mob
its own tiny `inventoryTemplate` whose slot(s) **reuse an existing `SlotFlags` bit**, and rely on
`whitelist`/`blacklist` (both plain `EntityWhitelist`, `tags:`/`components:`) at the **slot definition**
level to stop cross-compatibility — e.g. `pet_inventory_template.yml`'s `mask`/`head`/`eyes` slots all
`whitelist: tags: [PetWearable]`, and `human_inventory_template.yml`'s own `back` slot already
`blacklist: tags: [PetOnly]` (so a human can't wear a pet carrier as a backpack).

Respiral's horse saddle follows the exact same recipe (`Resources/Prototypes/_Respiral/InventoryTemplates/horse_inventory_template.yml`):
```yaml
- type: inventoryTemplate
  id: medievalHorse
  slots:
  - name: saddle
    slotFlags: BACK        # reused bit, matched against the item's Clothing.slots
    whitelist:
      tags: [MedievalSaddle]
```
Reusing `BACK` alone would let a human equip the saddle in their own `back` slot (same flag bit, no
inherent species check) — closed by adding the saddle's tag to the **human** template's existing `back`
slot blacklist (`Resources/Prototypes/InventoryTemplates/human_inventory_template.yml`, inside the
pre-existing `# Respiral-Start` block around that slot):
```yaml
- name: back
  ...
  blacklist:
    tags:
    - PetOnly
    - MedievalSaddle
```
`Clothing.Slots` on the item (`slots: [BACK]`, case-insensitive vs the enum) is the OTHER half of the
match — an item is equippable into a slot iff `(item.Clothing.Slots & slotDef.slotFlags) != 0` AND it
passes that slot's whitelist/blacklist. See [[mobs]] "Mounts & saddle slot" for the full horse writeup.

## Known Gaps

- Pants slot uses `uniform` placeholder texture — needs dedicated `pants.png` icons for all UI themes
- Base prototypes and clothing content now exist — see `clothing.md`
