using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;

namespace Content.Server._Respiral.NPC.Preconditions;

/// <summary>
///     Возвращает значение true, если процент оставшегося здоровья владельца находится на уровне или ниже заданного порога.
///     Здоровье рассчитывается исходя из порога смерти моба.
/// </summary>
public sealed partial class LowHealthPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;
    private MobThresholdSystem _thresholds = default!;
    private DamageableSystem _damageable = default!;

    [DataField(required: true)]
    public float Threshold;

    [DataField]
    public bool IncludeEquals = true;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _thresholds = sysManager.GetEntitySystem<MobThresholdSystem>();
        _damageable = sysManager.GetEntitySystem<DamageableSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, _entManager) ||
            !_entManager.TryGetComponent<DamageableComponent>(owner, out var damageable) ||
            !_thresholds.TryGetDeadPercentage(owner, _damageable.GetTotalDamage((owner, damageable)), out FixedPoint2? deadPercentage))
        {
            return false;
        }

        var clampedThreshold = Math.Clamp(Threshold, 0f, 1f);
        var remainingHealth = Math.Clamp(1f - deadPercentage.Value.Float(), 0f, 1f);

        return IncludeEquals
            ? remainingHealth <= clampedThreshold
            : remainingHealth < clampedThreshold;
    }
}