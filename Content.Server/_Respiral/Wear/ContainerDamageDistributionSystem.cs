using Content.Server_Respiral.Wear.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server_Respiral.Wear;

public sealed class ContainerDamageDistributionSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ContainerDamageDistributionComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
    }

    private void OnBeforeDamageChanged(
        EntityUid uid,
        ContainerDamageDistributionComponent component,
        ref BeforeDamageChangedEvent args)
    {
        if (component.DamageThreshold <= 0f)
            return;

        if (string.IsNullOrWhiteSpace(component.ContainerId))
            return;

        var damage = DamageSpecifier.GetPositive(args.Damage);
        if (damage.Empty)
            return;

        var totalDamage = damage.GetTotal().Float();
        var hitCount = (int)(totalDamage / component.DamageThreshold);

        if (hitCount <= 0)
            return;

        if (!_container.TryGetContainer(uid, component.ContainerId, out var container))
            return;

        List<EntityUid> targets = new(container.ContainedEntities.Count);
        foreach (var entity in container.ContainedEntities)
        {
            if (HasComp<DamageableComponent>(entity))
                targets.Add(entity);
        }

        if (targets.Count == 0)
            return;

        var distributedDamage = damage / hitCount;
        var origin = args.Origin ?? uid;

        for (var i = 0; i < hitCount; i++)
        {
            var target = targets[_random.Next(targets.Count)];
            _damageable.TryChangeDamage(target, distributedDamage, origin: origin);
        }
    }
}