using Robust.Shared.Prototypes;

namespace Content.Shared.Blacksmithing;

[Prototype]
public sealed partial class ForgingRecipePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("input", required: true)]
    public EntProtoId InputEntity { get; private set; } = default!;

    [DataField("output", required: true)]
    public EntProtoId OutputEntity { get; private set; } = default!;

    [DataField("failedOutput", required: true)]
    public EntProtoId FailedOutputEntity { get; private set; } = default!;

    [DataField]
    public List<StrikePattern> RequiredSequence { get; private set; } = new();

    [DataField]
    public int MinPerfectStrikes { get; private set; } = 0;

    [DataField]
    public int MaxBadStrikes { get; private set; } = 3;

    [DataField("multipliers")]
    public Dictionary<StrikeQuality, float> QualityMultipliers { get; private set; } = new()
    {
        { StrikeQuality.Perfect, 1.5f },
        { StrikeQuality.Good, 1.0f },
        { StrikeQuality.Bad, 0.3f }
    };

    [DataField]
    public (float Min, float Max) IdealTemperature { get; private set; } = (800f, 950f);
}

[Prototype]
public sealed partial class StrikeTypePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("strikeType", required: true)]
    public StrikeType Type { get; private set; }

    [DataField("timeWindow")]
    public (float Min, float Max) TimeWindow { get; private set; } = (0.8f, 1.8f);

    [DataField("rhythmBonus")]
    public float RhythmBonus { get; private set; } = 5f;
}

[DataRecord]
public sealed partial record StrikePattern
{
    [DataField("type")]
    public StrikeType Type { get; init; }

    [DataField("strikeId")]
    public ProtoId<StrikeTypePrototype>? StrikeId { get; init; }

    [DataField("amount")]
    public int Amount { get; init; } = 1;

    [DataField("timeWindow")]
    public (float Min, float Max) TimeWindow { get; init; } = (0.8f, 1.8f);

    [DataField("rhythmBonus")]
    public float RhythmBonus { get; init; } = 5f;
}
