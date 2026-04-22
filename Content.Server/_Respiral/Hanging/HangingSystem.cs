using Content.Server._Respiral.Hanging.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Respiral.Hanging;

public sealed class HangingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HangingComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<HangingComponent, UnstrappedEvent>(OnUnstrapped);
    }

    private void OnStrapped(EntityUid uid, HangingComponent component, ref StrappedEvent args)
    {
        component.HangingEntity = args.Buckle.Owner;
        component.IsActive = true;
        component.NextDamageTime = _timing.CurTime + TimeSpan.FromSeconds(component.DamageInterval);
    }

    private void OnUnstrapped(EntityUid uid, HangingComponent component, ref UnstrappedEvent args)
    {
        component.HangingEntity = null;
        component.IsActive = false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HangingComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.IsActive || comp.HangingEntity == null)
                continue;

            if (_timing.CurTime < comp.NextDamageTime)
                continue;

            _damageable.TryChangeDamage(comp.HangingEntity.Value, comp.Damage, origin: uid);

            comp.NextDamageTime = _timing.CurTime + TimeSpan.FromSeconds(comp.DamageInterval);
        }
    }
}
