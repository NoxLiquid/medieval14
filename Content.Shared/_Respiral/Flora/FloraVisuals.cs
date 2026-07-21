using Robust.Shared.Serialization;

namespace Content.Shared._Respiral.Flora;

[Serializable, NetSerializable]
public enum FloraVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public enum FloraState : byte
{
    Grown,
    Harvested
}
