using Robust.Shared.Audio;

namespace Content.Server._Respiral.Lock.Components;

[RegisterComponent]
public sealed partial class MedievalKeyDuplicatorComponent : Component
{
    [DataField]
    public string KeyId = string.Empty;

    [DataField]
    public float UseDuration = 15f;

    [DataField]
    public bool IsDuplicatorState = true;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Sound;

    [DataField]
    public SoundSpecifier? UseSound;
}
