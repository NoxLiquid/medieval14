using System.Numerics;
using Content.Shared._Respiral.Blacksmithing;
using Content.Shared._Respiral.Blacksmithing.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Respiral.Blacksmithing.Systems;

public sealed partial class ForgingSystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeatedIngotComponent, AppearanceChangeEvent>(OnForgeAppearanceChanged);
    }

    private void OnForgeAppearanceChanged(EntityUid uid, HeatedIngotComponent component, ref AppearanceChangeEvent args)
    {
        if (!_appearance.TryGetData(uid, IngotVisuals.Color, out Color color))
            color = Color.White;

        if (!_appearance.TryGetData(uid, IngotVisuals.Deformation, out Vector2 deformation))
            deformation = new Vector2(1.0f, 1.0f);

        if (!_appearance.TryGetData(uid, IngotVisuals.Glow, out bool glow))
            glow = false;

        _sprite.SetColor(uid, color);
        _sprite.SetScale(uid, deformation);
        _sprite.LayerSetVisible(uid, IngotVisuals.Glow, glow);
    }
}
