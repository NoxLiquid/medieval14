using Content.Server._Respiral.Magic.Mana.Components;
using Content.Shared._Respiral.TimeWindow;
using Content.Shared.Alert;

namespace Content.Server._Respiral.Magic.Mana;

/// <summary>
/// Мана: запас магической энергии, трата заклинаниями и восстановление со временем
/// через <see cref="TimedWindowSystem"/>. Server-authoritative — на клиент уходит только
/// индикатор-алерт (через сетевой AlertsComponent), само значение не реплицируется.
/// </summary>
public sealed partial class ManaSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private TimedWindowSystem _timedWindow = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ManaComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ManaComponent, ComponentStartup>(OnStartup);
    }

    private void OnMapInit(Entity<ManaComponent> ent, ref MapInitEvent args)
    {
        // Стартуем первый цикл регенерации, чтобы он не сработал мгновенно.
        // MapInit, а не ComponentStartup: RegenWindow — DataField, и его мутация на старте
        // ломала бы тест соответствия заспавненной сущности прототипу (он не вызывает MapInit).
        _timedWindow.Reset(ent.Comp.RegenWindow);
    }

    private void OnStartup(Entity<ManaComponent> ent, ref ComponentStartup args)
    {
        // Покрывает в том числе случай добавления компонента в рантайме (без MapInit).
        UpdateManaAlert((ent, ent.Comp));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ManaComponent>();
        while (query.MoveNext(out var uid, out var mana))
        {
            if (!_timedWindow.IsExpired(mana.RegenWindow))
                continue;

            // Перезапускаем окно даже когда мана полная — тогда следующая трата сразу пойдёт по таймеру.
            _timedWindow.Reset(mana.RegenWindow);

            if (mana.Mana >= mana.MaxMana)
                continue;

            ChangeMana((uid, mana), mana.RegenAmount);
        }
    }

    /// <summary>
    /// Хватает ли у сущности <paramref name="amount"/> маны.
    /// </summary>
    public bool HasMana(Entity<ManaComponent?> ent, float amount)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        return ent.Comp.Mana >= amount;
    }

    /// <summary>
    /// Пытается списать <paramref name="amount"/> маны. Возвращает false, если маны не хватает.
    /// </summary>
    public bool TryUseMana(Entity<ManaComponent?> ent, float amount)
    {
        if (!Resolve(ent, ref ent.Comp, false) || ent.Comp.Mana < amount)
            return false;

        ChangeMana((ent, ent.Comp), -amount);
        return true;
    }

    /// <summary>
    /// Изменяет ману на <paramref name="delta"/> (может быть отрицательным), с зажимом в [0; Max].
    /// </summary>
    public void ChangeMana(Entity<ManaComponent?> ent, float delta)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        SetMana((ent, ent.Comp), ent.Comp.Mana + delta);
    }

    /// <summary>
    /// Задаёт ману напрямую (с зажимом в [0; Max]).
    /// </summary>
    public void SetMana(Entity<ManaComponent?> ent, float value)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var clamped = Math.Clamp(value, 0f, ent.Comp.MaxMana);
        if (MathHelper.CloseTo(clamped, ent.Comp.Mana))
            return;

        var old = ent.Comp.Mana;
        ent.Comp.Mana = clamped;

        UpdateManaAlert((ent, ent.Comp));

        var ev = new ManaChangedEvent(old, clamped, ent.Comp.MaxMana);
        RaiseLocalEvent(ent, ref ev);
    }

    /// <summary>
    /// Обновляет иконку алерта маны под текущий уровень.
    /// </summary>
    public void UpdateManaAlert(Entity<ManaComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var alert = ent.Comp.ManaAlert;
        if (!_alerts.TryGet(alert, out _))
            return;

        var min = _alerts.GetMinSeverity(alert);
        var max = _alerts.GetMaxSeverity(alert);
        var ratio = ent.Comp.MaxMana <= 0f ? 0f : ent.Comp.Mana / ent.Comp.MaxMana;

        var severity = (short)Math.Clamp((int)MathF.Round(MathHelper.Lerp(min, max, ratio)), min, max);
        _alerts.ShowAlert(ent.Owner, alert, severity);
    }
}
