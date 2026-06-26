using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.DirtVisuals;

[RegisterComponent, NetworkedComponent]
public sealed partial class DirtableComponent : Component
{
    [IdDataField]
    public FixedPoint2 Threshold { get; set; } = 20;

    [DataField("dirtSprite")]
    public string DirtSpritePath { get; set; } = "_Respiral/Mobs/Effects/dirt_overlay.rsi";

    [DataField("state")]
    public string DirtState { get; set; } = "jumpsuit";

    [DataField("foldingState")]
    public string? FoldingDirtState { get; set; }

    [DataField("equippedState")]
    public string EquippedDirtState { get; set; } = "equipped-jumpsuit";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public FixedPoint2 CurrentDirtLevel { get; set; }

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public Color DirtColor { get; set; } = Color.White;

    [ViewVariables]
    public bool IsDirty => CurrentDirtLevel >= Threshold;
}

[Serializable, NetSerializable]
public sealed partial class DirtableComponentState : ComponentState
{
    public FixedPoint2 CurrentDirtLevel;
    public Color DirtColor;
    public bool IsDirty;

    public DirtableComponentState(FixedPoint2 currentDirtLevel, Color dirtColor, bool isDirty)
    {
        CurrentDirtLevel = currentDirtLevel;
        DirtColor = dirtColor;
        IsDirty = isDirty;
    }
}
