using Robust.Shared.Serialization;

namespace Content.Shared._Respiral.Interaction;

/// <summary>
/// Where to apply the nested effects: to the initiator, to the target, or to both.
/// </summary>
[Serializable, NetSerializable]
public enum EffectTarget : byte
{
    User,
    Target,
    Both
}
