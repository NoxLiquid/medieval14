using Content.Server.Destructible;
using Content.Shared.Construction;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;

namespace Content.Server._Respiral.Construction.Conditions;

[DataDefinition]
public sealed partial class MinHealthPercent : IGraphCondition
{
    [DataField]
    public float Value = 90f;

    public bool Condition(EntityUid uid, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent(uid, out DestructibleComponent? destructibleComp) ||
            !entMan.TryGetComponent(uid, out DamageableComponent? damageComp))
        {
            return true;
        }

        var destructionSys = entMan.System<DestructibleSystem>();
        var maxHealth = destructionSys.DestroyedAt(uid, destructibleComp);

        if (maxHealth <= 0)
            return true;

        var damageableSys = entMan.System<DamageableSystem>();
        var curHealth = maxHealth - damageableSys.GetTotalDamage((uid, damageComp));
        var percent = (float)(curHealth / maxHealth) * 100f;

        return percent >= Value;
    }

    public bool DoExamine(ExaminedEvent args)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();

        if (Condition(args.Examined, entMan))
            return false;

        args.PushMarkup(Loc.GetString("construction-examine-condition-low-health-percent",
            ("percent", Value)));

        return true;
    }

    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        yield return new ConstructionGuideEntry()
        {
            Localization = "construction-step-condition-low-health-percent"
        };
    }
}
