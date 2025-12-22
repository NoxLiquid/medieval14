using Robust.Shared.Serialization;

namespace Content.Shared.Blacksmithing;

[RegisterComponent]
public sealed partial class EntityQualityComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public EntityQuality Quality = EntityQuality.Rough;

    [DataField("producerMark")]
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
