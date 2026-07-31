using Content.Shared._Respiral.TimeWindow;
using Robust.Shared.Prototypes;

namespace Content.Shared._Respiral.Precipitation;

/// <summary>
/// Respiral: тип осадка (напр. дождь). Визуал и звук берутся из weather-прототипа,
/// сам осадок задаёт лишь тайминги.
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
    /// Длительность осадка: случайное время из окна [Min, Max].
    /// </summary>
    [DataField]
    public TimedWindow Duration = new(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(3));
}
