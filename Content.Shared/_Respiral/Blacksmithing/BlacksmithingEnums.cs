using Robust.Shared.Serialization;

namespace Content.Shared._Respiral.Blacksmithing;

[Serializable, NetSerializable]
public enum IngotVisuals : byte
{
    Color,
    Glow,
    Deformation
}

[Serializable, NetSerializable]
public enum StrikeType : byte
{
    Light,
    Medium,
    Heavy
}

[Serializable, NetSerializable]
public enum StrikeQuality : byte
{
    Bad,
    Good,
    Perfect
}
