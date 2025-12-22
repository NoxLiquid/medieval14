using Robust.Shared.GameStates;

namespace Content.Shared.Blacksmithing.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BlacksmithingProcessComponent : Component
{
    [DataField("strikeSequence")]
    public List<StrikeData> StrikeSequence = new();

    [DataField("currentStage")]
    public int CurrentStage = 0;

    [ViewVariables]
    public TimeSpan LastStrikeTime;
}

[DataRecord]
public sealed partial record StrikeData
{
    [DataField("type")]
    public StrikeType Type { get; init; }

    [DataField("time")]
    public TimeSpan Time { get; init; }

    [DataField("temperature")]
    public float Temperature { get; init; }

    [DataField("timeFromPrevious")]
    public double TimeFromPrevious { get; init; }
}
