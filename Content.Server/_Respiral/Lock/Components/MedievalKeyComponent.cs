using Robust.Shared.Audio;

namespace Content.Server._Respiral.Lock.Components;

[RegisterComponent]
public sealed partial class MedievalKeyComponent : Component
{
    [DataField]
    public string KeyId = string.Empty;

    [DataField]
    public float UseDuration = 3f;

    [DataField]
    public bool IsLockpick = false;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Sound;

    [DataField]
    public SoundSpecifier? UseSound;
}
