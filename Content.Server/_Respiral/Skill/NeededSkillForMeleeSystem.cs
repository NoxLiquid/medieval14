using Content.Server._Respiral.Skill.Components;
using Content.Shared._Respiral.Skills;
using Content.Shared._Respiral.Skills.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server._Respiral.Skill;

public sealed partial class NeededSkillForMeleeSystem : EntitySystem
{
    [Dependency] private SkillSystem _skillSystem = default!;
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NeededSkillForMeleeComponent, MeleeHitEvent>(OnNeededSkillForMeleeHit);
        SubscribeLocalEvent<NeededSkillForMeleeComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, NeededSkillForMeleeComponent component, ExaminedEvent args)
    {
        if (component.NeededSkillsProgress.Count == 0)
            return;

        var skills = new List<ProtoId<SkillPrototype>>(component.NeededSkillsProgress.Keys);

        args.PushMarkup(Loc.GetString("skill-exm-needed-skills",
            ("skills", _skillSystem.GetSkillNames(skills))));
    }

    private void OnNeededSkillForMeleeHit(EntityUid uid, NeededSkillForMeleeComponent component, ref MeleeHitEvent args)
    {
        // Урон: за каждый навык владения оружием множитель растёт линейно к требуемому уровню
        // и не превышает его — полный урон только при владении оружием не хуже требуемого уровня.
        var damage = args.BaseDamage;

        foreach (var (skill, requiredLevel) in component.NeededSkillsProgress)
        {
            var requiredProgress = SkillKnowledge.GetMinProgress(requiredLevel);
            if (requiredProgress <= 0f)
                continue; // Vague ничего не требует — множитель не снижаем

            var actualProgress = _skillSystem.GetSkillProgress(args.User, skill);
            damage *= Math.Min(requiredProgress, actualProgress / requiredProgress);
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
