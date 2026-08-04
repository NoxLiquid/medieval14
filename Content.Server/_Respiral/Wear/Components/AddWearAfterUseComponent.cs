using Content.Shared.Damage;
using Content.Shared.Whitelist;

namespace Content.Server_Respiral.Wear.Components;

[RegisterComponent]
public sealed partial class AddWearAfterUseComponent : Component
{
    /// <summary>
    ///     Урон при использовании
    /// </summary>
    [DataField]
    public DamageSpecifier Damage = new DamageSpecifier();

    [DataField]
    public WearTrigger Triggers = WearTrigger.None;

    [DataField]
    public EntityWhitelist? Whitelist;
}

/// <summary>
///     Какие типы действий могут вызывать износ предмета.
/// </summary>
[Flags]
public enum WearTrigger
{
    None = 0,
    MeleeHit = 1 << 0,
    Interact = 1 << 1,
    TileTool = 1 << 2,
    Learn = 1 << 3,
    RCD = 1 << 4,
    Key = 1 << 5,
    KeyDuplicator = 1 << 6,
    Tool = 1 << 7,
}
