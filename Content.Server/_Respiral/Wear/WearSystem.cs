using Content.Server_Respiral.Wear.Components;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Triggers;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Robust.Server.Audio;

namespace Content.Server_Respiral.Wear;

public sealed partial class WearSystem : EntitySystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private DamageableSystem _damage = default!;
    public override void Initialize()
    {
        base.Initialize();

        BeforeInitialize();

        SubscribeLocalEvent<WearComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, WearComponent component, ExaminedEvent args)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable)
            || !TryComp<DestructibleComponent>(uid, out var destructible))
            return;

        var remainingBreak = GetThresholdAt(destructible, ThresholdActs.Breakage) - damageable.TotalDamage;
        var remainingDestroy = GetThresholdAt(destructible, ThresholdActs.Destruction) - damageable.TotalDamage;

        if (remainingBreak <= FixedPoint2.Zero || component.State == WearState.Broken)
        {
            args.PushMarkup(Loc.GetString("wear-exm-broken"));
            args.PushMarkup(Loc.GetString("wear-exm-destroy-info", ("points", Math.Round(FixedPoint2.Max(remainingDestroy, FixedPoint2.Zero).Float()).ToString())));
            return;
        }

        args.PushMarkup(Loc.GetString(
            "wear-exm-info",
            ("breakPoints", Math.Round(FixedPoint2.Max(remainingBreak, FixedPoint2.Zero).Float()).ToString()),
            ("destroyPoints", Math.Round(FixedPoint2.Max(remainingDestroy, FixedPoint2.Zero).Float()).ToString())));
    }

    public void AddWear(EntityUid uid, DamageSpecifier damage, WearComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        _damage.TryChangeDamage(uid,
                damage,
                origin: uid,
                ignoreResistances: false,
                interruptsDoAfters: false);

        if (component.Sound != null)
            _audio.PlayPvs(component.Sound, Transform(uid).Coordinates);

    }

    public FixedPoint2 GetRemainingDurability(EntityUid uid, DamageableComponent? damageable = null, DestructibleComponent? destructible = null)
    {
        if (!Resolve(uid, ref damageable))
            return FixedPoint2.Zero;

        if (!Resolve(uid, ref destructible))
            return FixedPoint2.Zero;

        var destroyedAt = GetThresholdAt(destructible, ThresholdActs.Destruction);

        return FixedPoint2.Max(destroyedAt - damageable.TotalDamage, FixedPoint2.Zero);
    }

    public FixedPoint2 GetRemainingBreakDurability(EntityUid uid, DamageableComponent? damageable = null, DestructibleComponent? destructible = null)
    {
        if (!Resolve(uid, ref damageable))
            return FixedPoint2.Zero;

        if (!Resolve(uid, ref destructible))
            return FixedPoint2.Zero;

        var brokenAt = GetThresholdAt(destructible, ThresholdActs.Breakage);

        return FixedPoint2.Max(brokenAt - damageable.TotalDamage, FixedPoint2.Zero);
    }

    private static FixedPoint2 GetThresholdAt(DestructibleComponent destructible, ThresholdActs act)
    {
        var damageNeeded = FixedPoint2.MaxValue;

        foreach (var threshold in destructible.Thresholds)
        {
            if (threshold.Trigger is not DamageTrigger trigger)
                continue;

            foreach (var behavior in threshold.Behaviors)
            {
                if (behavior is DoActsBehavior actBehavior && actBehavior.HasAct(act))
                    damageNeeded = FixedPoint2.Min(damageNeeded, trigger.Damage);
            }
        }

        return damageNeeded;
    }

}
