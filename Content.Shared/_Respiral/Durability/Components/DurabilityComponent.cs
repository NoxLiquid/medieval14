using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Durability;

[RegisterComponent]
public sealed partial class DurabilityComponent : Component
{
    [DataField("currentDurability")]
    public float CurrentDurability = 100;

    [DataField("maxDurability")]
    public float MaxDurability = 100;

    [DataField("degradeOnUse")]
    public bool DegradeOnUse = true;

    [DataField("currentDurabilityLoss")]
    public float CurrentDurabilityLoss = 1;

    [DataField("minLossMultiplier")]
    public float MinLossMultiplier = 0.8f;

    [DataField("maxLossMultiplier")]
    public float MaxLossMultiplier = 1.5f;

    [DataField("destroyOnBreak")]
    public bool DestroyOnBreak = true;

    [DataField("destroyObject")]
    public EntProtoId? DestroyObject;

    [DataField("breakSound")]
    public SoundSpecifier? BreakSound;
}
