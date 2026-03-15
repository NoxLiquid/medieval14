using Content.Server._Respiral.Skill.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server._Respiral.Skill;

public sealed class MeleeSkillSystem : EntitySystem
{
    [Dependency] private readonly SkillSystem _skillSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MeleeSkillComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(EntityUid uid, MeleeSkillComponent component, ref MeleeHitEvent args)
    {
        float bestModifier = component.DefaultModifier;

        foreach (var skill in component.Skills)
        {
            if (component.DamageModifiers.TryGetValue(skill, out var modifier))
                bestModifier = Math.Max(bestModifier * _skillSystem.GetSkillProgress(args.User, skill), modifier * _skillSystem.GetSkillProgress(args.User, skill));
            else
                bestModifier = bestModifier * _skillSystem.GetSkillProgress(args.User, skill);
        }

        args.BonusDamage = args.BaseDamage * bestModifier - args.BaseDamage;
    }
}
