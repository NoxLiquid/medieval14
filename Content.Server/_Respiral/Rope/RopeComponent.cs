using Robust.Shared.Utility;

namespace Content.Server._Respiral.Rope;

[RegisterComponent]
public sealed partial class RopeComponent : Component
{
    [ViewVariables]
    public EntityUid? PendingTarget;

    [DataField]
    public TimeSpan TieDelay = TimeSpan.FromSeconds(3);

    [DataField]
    public float TetherDistanceDivisor = 2.5f;

    [DataField]
    public SpriteSpecifier RopeVisual =
        new SpriteSpecifier.Rsi(new ResPath("Objects/Weapons/Guns/Launchers/grappling_gun.rsi"), "rope");
}
