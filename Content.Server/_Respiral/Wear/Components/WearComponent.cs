using Robust.Shared.Audio;

namespace Content.Server_Respiral.Wear.Components;

[RegisterComponent]
public sealed partial class WearComponent : Component
{
    /// <summary>
    ///     Звук при износе предмета
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? Sound = default!;

    [DataField]
    public WearState State = WearState.Normal;
}

/// <summary>
///     Текущее состояние износа предмета
/// </summary>
[Flags]
public enum WearState
{
    Invalid = 0,
    Normal = 1 << 1,
    Broken = 1 << 2
}