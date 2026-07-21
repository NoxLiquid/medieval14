using Content.Shared.Clothing.Components;
using Content.Shared.DisplacementMap;
using Content.Shared.Inventory;
using Robust.Client.GameObjects;

namespace Content.Client._Respiral.Dirt;

/// <summary>Точка расширения: слой грязи на надетой одежде. Поднимается на предмете; <see cref="Index"/> обновляется.</summary>
[ByRefEvent]
public struct AddEquipmentDirtLayerEvent
{
    public readonly Entity<SpriteComponent> Equipee;
    public readonly ClothingComponent Clothing;
    public readonly InventoryComponent Inventory;
    public readonly SlotDefinition SlotDef;
    public readonly DisplacementData? DisplacementData;
    public readonly bool SlotLayerExists;
    public readonly HashSet<string> RevealedLayers;
    public int Index;

    public AddEquipmentDirtLayerEvent(
        Entity<SpriteComponent> equipee,
        ClothingComponent clothing,
        InventoryComponent inventory,
        SlotDefinition slotDef,
        DisplacementData? displacementData,
        bool slotLayerExists,
        HashSet<string> revealedLayers,
        int index)
    {
        Equipee = equipee;
        Clothing = clothing;
        Inventory = inventory;
        SlotDef = slotDef;
        DisplacementData = displacementData;
        SlotLayerExists = slotLayerExists;
        RevealedLayers = revealedLayers;
        Index = index;
    }
}
