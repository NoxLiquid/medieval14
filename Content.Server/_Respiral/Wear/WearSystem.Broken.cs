using Content.Shared.Tools.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Server.Destructible;
using Content.Server_Respiral.Wear.Components;

namespace Content.Server_Respiral.Wear;

public sealed partial class WearSystem : EntitySystem
{
    private void BeforeInitialize()
    {
        SubscribeLocalEvent<WearComponent, ComponentStartup>(OnStartup, after: [typeof(DestructibleSystem)]);
        SubscribeLocalEvent<WearComponent, BreakageEventArgs>(OnBreakage);
        SubscribeLocalEvent<WearComponent, DamageChangedEvent>(OnDamageChanged, after: [typeof(DestructibleSystem)]);

        SubscribeLocalEvent<WearComponent, AttemptMeleeEvent>(OnAttemptMelee);
        SubscribeLocalEvent<WearComponent, AttemptMeleeThrowOnHitEvent>(OnAttemptMeleeThrow);
        SubscribeLocalEvent<WearComponent, ToolUseAttemptEvent>(OnToolUseAttempt);
        SubscribeLocalEvent<WearComponent, AttemptSimpleToolUseEvent>(OnAttemptSimpleTool);
    }

    private void OnStartup(Entity<WearComponent> ent, ref ComponentStartup args)
    {
        UpdateWearState(ent, TryComp<DestructibleComponent>(ent, out var destructible) ? destructible : null);
    }

    private void OnBreakage(Entity<WearComponent> ent, ref BreakageEventArgs args)
    {
        ent.Comp.State = WearState.Broken;
    }

    private void OnDamageChanged(Entity<WearComponent> ent, ref DamageChangedEvent args)
    {
        UpdateWearState(ent);
    }

    private void UpdateWearState(Entity<WearComponent> ent, DestructibleComponent? destructible = null, DamageableComponent? damageable = null)
    {
        var isBroken = false;

        if (Resolve(ent, ref destructible, false)
            && Resolve(ent, ref damageable, false))
        {
            var breakAt = GetThresholdAt(destructible, ThresholdActs.Breakage);
            isBroken = breakAt != FixedPoint2.MaxValue && _damage.GetTotalDamage((ent.Owner, damageable)) >= breakAt;
        }

        ent.Comp.State = isBroken ? WearState.Broken : WearState.Normal;
    }

    private void OnAttemptMelee(Entity<WearComponent> ent, ref AttemptMeleeEvent args)
    {
        if (ent.Comp.State == WearState.Broken)
            args.Cancelled = true;
    }

    private void OnAttemptMeleeThrow(Entity<WearComponent> ent, ref AttemptMeleeThrowOnHitEvent args)
    {
        if (ent.Comp.State == WearState.Broken)
            args.Cancelled = true;
    }

    private void OnToolUseAttempt(Entity<WearComponent> ent, ref ToolUseAttemptEvent args)
    {
        if (ent.Comp.State == WearState.Broken)
            args.Cancel();
    }

    private void OnAttemptSimpleTool(Entity<WearComponent> ent, ref AttemptSimpleToolUseEvent args)
    {
        if (ent.Comp.State == WearState.Broken)
            args.Cancelled = true;
    }
}
