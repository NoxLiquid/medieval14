using Content.Server._Respiral.Skill.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;

namespace Content.Server._Respiral.Skill;

public sealed class MeleeSkillSystem : EntitySystem
{
    [Dependency] private SkillSystem _skillSystem = default!;
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MeleeSkillComponent, MeleeHitEvent>(OnMeleeSkillHit);
    }

    private void OnMeleeSkillHit(EntityUid uid, MeleeSkillComponent component, ref MeleeHitEvent args)
    {
        // Урон
        var damage = args.BaseDamage;

        foreach (var skill in component.NeededSkills)
        {
            damage *= _skillSystem.GetSkillProgress(args.User, skill);
        }

        args.BonusDamage = damage;


        // Изучение
        foreach (var entity in args.HitEntities)
        {
            if (args.User == entity)
                continue;

            // без MobStateSystem, иначе не пройдёт проверку, если нет компонента MobState
            if (TryComp<MobStateComponent>(entity, out var mobStateComponent)
                && mobStateComponent.CurrentState != MobState.Alive)
                continue;

            var modifier = 0f;
            foreach (var learnMod in component.LearnModifiers)
            {
                if (_entityWhitelist.IsValid(learnMod.Whitelist, entity))
                    modifier += learnMod.Modifier;
            }

            if (modifier == 0f)
                continue;

            foreach (var skillDict in component.Points)
            {
                foreach (var (skill, value) in skillDict)
                {
                    if (_skillSystem.CanLearn(args.User, skill))
                        _skillSystem.AddSkillProgress(args.User, skill, value * modifier);
                }
            }
        }
    }
}
