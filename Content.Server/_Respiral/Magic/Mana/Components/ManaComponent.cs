using Content.Shared._Respiral.TimeWindow;
using Content.Shared.Alert;
using Robust.Shared.Prototypes;

namespace Content.Server._Respiral.Magic.Mana.Components;

/// <summary>
/// Хранит запас маны сущности — магической энергии, которую тратят заклинания.
/// Мана восстанавливается со временем порциями через <see cref="RegenWindow"/>.
/// Значение server-authoritative; на клиент уходит только индикатор-алерт (через AlertsComponent).
/// </summary>
[RegisterComponent]
public sealed partial class ManaComponent : Component
{
    /// <summary>
    /// Текущий запас маны.
    /// </summary>
    [DataField]
    public float Mana;

    /// <summary>
    /// Максимальный запас маны.
    /// </summary>
    [DataField]
    public float MaxMana = 100f;

    /// <summary>
    /// Сколько маны восстанавливается за одно срабатывание <see cref="RegenWindow"/>.
    /// </summary>
    [DataField]
    public float RegenAmount = 5f;

    /// <summary>
    /// Окно регенерации: как часто (сек) начисляется <see cref="RegenAmount"/>.
    /// Min == Max — фиксированный интервал.
    /// </summary>
    [DataField]
    public TimedWindow RegenWindow = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

    /// <summary>
    /// Алерт-индикатор уровня маны.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> ManaAlert = "Mana";
}

/// <summary>
/// Вызывается при изменении маны сущности.
/// </summary>
[ByRefEvent]
public readonly record struct ManaChangedEvent(float OldMana, float NewMana, float MaxMana);
