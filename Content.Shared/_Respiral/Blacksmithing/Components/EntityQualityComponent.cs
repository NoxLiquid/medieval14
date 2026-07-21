using Robust.Shared.Serialization;

namespace Content.Shared._Respiral.Blacksmithing.Components;

[RegisterComponent]
public sealed partial class EntityQualityComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public EntityQuality Quality = EntityQuality.Rough;

    [DataField]
    public string ProducerMark = string.Empty;
}

[Serializable, NetSerializable]
public enum EntityQuality : byte
{
    Rough,
    Normal,
    Good,
    Master,
    Masterpiece
}
