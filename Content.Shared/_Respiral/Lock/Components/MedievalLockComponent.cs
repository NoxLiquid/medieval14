using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Respiral.Lock.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedMedievalLockSystem))]
public sealed partial class MedievalLockComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Locked = false;

    [DataField]
    public string KeyId = string.Empty;

    [DataField]
    public SoundSpecifier? AttachSound;
}
