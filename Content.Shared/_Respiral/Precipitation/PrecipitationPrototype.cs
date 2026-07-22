using Content.Shared._Respiral.TimeWindow;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Respiral.Precipitation;

/// <summary>
/// Respiral: тип осадка (напр. дождь). Визуал и звук берутся из weather-прототипа,
/// сам осадок описывает разливаемый реагент и тайминги.
/// </summary>
[Prototype]
public sealed partial class PrecipitationPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Weather status effect (parallax + звук), который включается на карте на время осадка.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Weather;

    /// <summary>
    /// Реагент, который разливается на тайлы.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    /// <summary>
    /// Сколько единиц реагента лить на один тайл за раз.
    /// </summary>
    [DataField]
    public FixedPoint2 PuddleAmount = FixedPoint2.New(5);

    /// <summary>
    /// Сколько случайных тайлов мочить за один тик разлива.
    /// </summary>
    [DataField]
    public int TilesPerTick = 5;

    /// <summary>
    /// Интервал между тиками разлива осадков по тайлу.
    /// </summary>
    [DataField]
    public TimeSpan SpillInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Длительность осадка: случайное время из окна [Min, Max].
    /// </summary>
    [DataField]
    public TimedWindow Duration = new(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(3));
}
