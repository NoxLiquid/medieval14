using Content.Shared._Respiral.Mount;
using Content.Shared._Respiral.Mount.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Map;

namespace Content.Client._Respiral.Mount;
public sealed partial class MountSystem : SharedMountSystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private IEyeManager _eye = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MountableComponent, MoveEvent>(OnMountMove);
    }

    private void OnMountMove(Entity<MountableComponent> ent, ref MoveEvent args)
    {
        if (ent.Comp.Rider is not { } rider)
            return;

        UpdateDepth(ent, rider);
    }

    protected override void OnRiderMounted(EntityUid mount, EntityUid rider)
    {
        UpdateDepth(mount, rider);
    }

    protected override void OnRiderDismounted(EntityUid mount, EntityUid rider)
    {
        if (TryComp<RiderDepthComponent>(rider, out var depth) && TryComp<SpriteComponent>(rider, out var sprite))
            _sprite.SetDrawDepth((rider, sprite), depth.OriginalDrawDepth);

        RemComp<RiderDepthComponent>(rider);
    }

    private void UpdateDepth(EntityUid mount, EntityUid rider)
    {
        if (!TryComp<SpriteComponent>(mount, out var mountSprite) || !TryComp<SpriteComponent>(rider, out var riderSprite))
            return;

        if (!TryComp<RiderDepthComponent>(rider, out var depth))
        {
            depth = AddComp<RiderDepthComponent>(rider);
            depth.OriginalDrawDepth = riderSprite.DrawDepth;
        }

        var angle = _xform.GetWorldRotation(mount) + _eye.CurrentEye.Rotation;
        var isSouth = angle.GetCardinalDir() == Direction.South;

        var newDepth = isSouth ? mountSprite.DrawDepth - 1 : mountSprite.DrawDepth + 1;
        _sprite.SetDrawDepth((rider, riderSprite), newDepth);
    }
}
