using Content.Client.Chemistry.Visualizers;
using Content.Shared._Respiral.Barrel;
using Content.Shared.Chemistry;
using Content.Shared.Storage;
using Robust.Client.GameObjects;

namespace Content.Client._Respiral.Barrel;

public sealed class BarrelVisualizerSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BarrelComponent, AppearanceChangeEvent>(
            OnAppearanceChange,
            after: [typeof(SolutionContainerVisualsSystem)]);
    }

    private void OnAppearanceChange(Entity<BarrelComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_sprite.LayerMapTryGet((ent, args.Sprite), SolutionContainerLayers.Fill, out _, false))
            return;

        var open = _appearance.TryGetData<bool>(ent, StorageVisuals.Open, out var isOpen, args.Component) && isOpen;

        if (!open)
            _sprite.LayerSetVisible((ent, args.Sprite), SolutionContainerLayers.Fill, false);
    }
}
