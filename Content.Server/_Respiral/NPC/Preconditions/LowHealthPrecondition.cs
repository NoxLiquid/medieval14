using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;

namespace Content._Respiral.NPC.Preconditions;

/// <summary>
/// Returns true if the owner's remaining health percentage is at or below the configured threshold.
/// Health is calculated against the mob's dead threshold.
/// </summary>
public sealed partial class LowHealthPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;

    [DataField(required: true)]
    public float Threshold;

    [DataField]
    public bool IncludeEquals = true;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, _entManager) ||
            !_entManager.TryGetComponent<DamageableComponent>(owner, out var damageable) ||
            !_thresholds.TryGetDeadPercentage(owner, damageable.TotalDamage, out FixedPoint2? deadPercentage))
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