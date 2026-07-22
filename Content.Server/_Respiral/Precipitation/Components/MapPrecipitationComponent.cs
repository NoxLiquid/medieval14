using Content.Shared._Respiral.Precipitation;
using Content.Shared._Respiral.TimeWindow;
using Robust.Shared.Prototypes;

namespace Content.Server._Respiral.Precipitation.Components;

/// <summary>
/// Respiral: вешается на карту, где могут идти осадки. Управляет авто-циклом:
/// сухой период (<see cref="Interval"/>) → случайный осадок из <see cref="Possible"/> → снова сухо.
/// </summary>
[RegisterComponent]
public sealed partial class MapPrecipitationComponent : Component
{
    /// <summary>
    /// Возможные осадки на этой карте. При старте выбирается случайный.
    /// </summary>
    [DataField]
    public List<ProtoId<PrecipitationPrototype>> Possible = new();

    /// <summary>
    /// Окно сухого периода между осадками.
    /// </summary>
    [DataField]
    public TimedWindow Interval = new(TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(8));

    /// <summary>
    /// Текущий активный осадок (null — сухо).
    /// </summary>
    [ViewVariables]
    public ProtoId<PrecipitationPrototype>? Active;

    /// <summary>
    /// Когда закончится текущий осадок.
    /// </summary>
    [ViewVariables]
    public TimeSpan EndTime;

    /// <summary>
    /// Когда полить тайлы в следующий раз.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextSpill;
}
