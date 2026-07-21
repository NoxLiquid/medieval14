using Content.Client.Clothing;
using Content.Client.DisplacementMap;
using Content.Shared._Respiral.Dirt.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.DisplacementMap;
using Content.Shared.Foldable;
using Content.Shared.Inventory;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client._Respiral.Dirt;

public sealed partial class DirtVisualsSystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private ClientClothingSystem _clothing = default!;
    [Dependency] private IResourceCache _cache = default!;
    [Dependency] private DisplacementMapSystem _displacement = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DirtableComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, DirtableComponent comp, ref ComponentHandleState args)
    {
        if (args.Current is not DirtableComponentState state)
            return;

        comp.CurrentDirtLevel = state.CurrentDirtLevel;
        comp.DirtColor = state.DirtColor;
        UpdateDirtVisuals(uid, comp);
    }

    private void UpdateDirtVisuals(EntityUid uid, DirtableComponent comp)
    {
        if (!HasComp<SpriteComponent>(uid))
            return;

        var isFolded = false;
        if (HasComp<AppearanceComponent>(uid) && _appearance.TryGetData<bool>(uid, FoldableSystem.FoldedVisuals.State, out var folded))
            isFolded = folded;

        var layerKey = $"dirt_{uid}";
        var dirtState = isFolded && !string.IsNullOrEmpty(comp.FoldingDirtState)
            ? comp.FoldingDirtState
            : comp.DirtState;

        if (comp.IsDirty)
        {
            if (!_sprite.LayerMapTryGet(uid, layerKey, out var layerIndex, false))
            {
                layerIndex = _sprite.AddLayer(uid, new SpriteSpecifier.Rsi(
                    new ResPath(comp.DirtSpritePath),
                    dirtState
                ));
                _sprite.LayerMapSet(uid, layerKey, layerIndex);
            }

            _sprite.LayerSetVisible(uid, layerIndex, true);
            _sprite.LayerSetColor(uid, layerIndex, comp.DirtColor);

            _sprite.LayerSetRsiState(uid, layerIndex, dirtState);
        }
        else if (_sprite.LayerMapTryGet(uid, layerKey, out var layerIndex, false))
        {
            _sprite.LayerSetVisible(uid, layerIndex, false);
        }

        if (TryComp(Transform(uid).ParentUid, out InventoryComponent? inventory))
            _clothing.InitClothing(Transform(uid).ParentUid, inventory);
    }

    /// <summary>
    /// Добавляет слой грязи поверх надетой одежды при отрисовке экипировки носителя.
    /// </summary>
    /// <param name="equipee">Носитель со своим спрайтом, в который добавляется слой.</param>
    /// <param name="equipment">Предмет одежды (потенциально <see cref="DirtableComponent"/>).</param>
    /// <param name="index">Текущий индекс слоя; сдвигается, если слой грязи был добавлен.</param>
    public void TryAddEquipmentDirtLayer(
        Entity<SpriteComponent> equipee,
        EntityUid equipment,
        ClothingComponent clothing,
        InventoryComponent inventory,
        SlotDefinition slotDef,
        DisplacementData? displacementData,
        bool slotLayerExists,
        HashSet<string> revealedLayers,
        ref int index)
    {
        if (!TryComp<DirtableComponent>(equipment, out var dirtable)
            || !dirtable.IsDirty
            || dirtable.DirtSpritePath == null
            || revealedLayers.Contains($"dirt_{equipment}"))
            return;

        var dirtRsi = _cache.GetResource<RSIResource>(
            SpriteSpecifierSerializer.TextureRoot / dirtable.DirtSpritePath).RSI;

        var state = dirtable.EquippedDirtState;
        if (!string.IsNullOrEmpty(clothing.EquippedPrefix))
            state = $"{clothing.EquippedPrefix}-{state}";
        if (inventory.SpeciesId != null && dirtRsi.TryGetState($"{state}-{inventory.SpeciesId}", out _))
            state = $"{state}-{inventory.SpeciesId}";

        if (!dirtRsi.TryGetState(state, out _))
            return;

        var dirtLayer = new PrototypeLayerData
        {
            RsiPath = dirtable.DirtSpritePath,
            State = state,
            Color = dirtable.DirtColor
        };

        var dirtKey = $"dirt_{equipment}";
        if (slotLayerExists)
        {
            index++;
            _sprite.AddBlankLayer(equipee, index);
            _sprite.LayerMapSet(equipee.AsNullable(), dirtKey, index);
        }
        else
        {
            index = _sprite.LayerMapReserve(equipee.AsNullable(), dirtKey);
        }

        // Accounting for a displacements
        if (equipee.Comp[index] is not Layer layer)
            return;

        _sprite.LayerSetData(equipee.AsNullable(), index, dirtLayer);
        _sprite.LayerSetOffset(layer, layer.Offset + slotDef.Offset);
        revealedLayers.Add(dirtKey);

        if (displacementData is not null
            && _displacement.TryAddDisplacement(displacementData, equipee, index, dirtKey, out var displacementKey))
        {
            revealedLayers.Add(displacementKey);
            index++;
        }
    }
}
