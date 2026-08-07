using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._Respiral.Materials;

public sealed partial class MaterialColorSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MaterialColorVisualsComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<MaterialColorVisualsComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
        {
            foreach (var (layerKey, materialId) in ent.Comp.Layers)
            {
                if (!_proto.TryIndex(materialId, out var material))
                    continue;

                if (!_sprite.LayerMapTryGet((ent, sprite), layerKey, out var index, false))
                    continue;

                _sprite.LayerSetColor((ent, sprite), index, material.Color);
            }
        }

        if (ent.Comp.Inhand is not { } inhandId
            || !_proto.TryIndex(inhandId, out var inhandMaterial)
            || !TryComp<ItemComponent>(ent, out var item))
            return;

        var changed = false;
        foreach (var layers in item.InhandVisuals.Values)
        {
            foreach (var layer in layers)
            {
                layer.Color = inhandMaterial.Color;
                changed = true;
            }
        }

        if (changed)
            _item.VisualsChanged(ent);
    }
}
