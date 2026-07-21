using Content.Shared._Respiral.TimeWindow;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Respiral.Flora.Components;

[RegisterComponent]
public sealed partial class FloraComponent : Component
{
    /// <summary>
    /// Окно отрастания после сбора: случайное время в диапазоне [min, max].
    /// Отсчёт запускается в момент сбора через <see cref="TimedWindowSystem.Reset"/>.
    /// </summary>
    [DataField]
    public TimedWindow GrowthWindow = new(TimeSpan.FromSeconds(1200), TimeSpan.FromSeconds(1800));

    [DataField]
    public bool IsGrown = true;

    [DataField(required: true)]
    public string HarvestPrototype { get; set; } = string.Empty;

    [DataField]
    public int MinYield = 1;

    [DataField]
    public int MaxYield = 3;

    [DataField("specialTool")]
    public ProtoId<ToolQualityPrototype>? SpecialTool = null;

    [DataField]
    public float HarvestDuration = 2f;

    public SoundSpecifier? HarvestSound = null;
}
