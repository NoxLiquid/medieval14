using Content.Shared._Respiral.Burning;
using Content.Shared._Respiral.Burning.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Respiral.Burning;

public sealed partial class FuelBurnerSystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FuelBurnerComponent, AppearanceChangeEvent>(OnBurnerAppearanceChanged);
    }

    private void OnBurnerAppearanceChanged(EntityUid uid, FuelBurnerComponent component, ref AppearanceChangeEvent args)
    {
        if (!_appearance.TryGetData(uid, BurnerVisuals.Active, out bool active))
            active = false;

        _sprite.LayerSetVisible(uid, BurnerVisuals.Active, active);
    }
}
