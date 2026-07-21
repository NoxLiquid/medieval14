using Content.Server._Respiral.Skill.Components;
using Content.Shared._Respiral.Skill.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Robust.Shared.Prototypes;

namespace Content.Server._Respiral.Skill;

public sealed partial class NeededSkillForInteractUsingSystem : EntitySystem
{
    [Dependency] private SkillSystem _skillSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        // BeforeRangedInteractEvent поднимается движком на используемом предмете (Used) до того,
        // как взаимодействие обработает цель. Блокируем применение предмета, если у пользователя
        // нет требуемых навыков (CheckRequiredSkills сам показывает попап).
        SubscribeLocalEvent<NeededSkillForInteractUsingComponent, BeforeRangedInteractEvent>(OnBeforeRangedInteract);
        SubscribeLocalEvent<NeededSkillForInteractUsingComponent, ExaminedEvent>(OnExamine);
    }

    private void OnBeforeRangedInteract(EntityUid uid, NeededSkillForInteractUsingComponent component, BeforeRangedInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!_skillSystem.CheckRequiredSkillsProgress(args.User, component.NeededSkillsProgress))
            args.Handled = true;
    }

    private void OnExamine(EntityUid uid, NeededSkillForInteractUsingComponent component, ExaminedEvent args)
    {
        if (component.NeededSkillsProgress.Count == 0)
            return;

        var skills = new List<ProtoId<SkillPrototype>>(component.NeededSkillsProgress.Keys);

        args.PushMarkup(Loc.GetString("skill-exm-needed-skills",
            ("skills", _skillSystem.GetSkillNames(skills))));
    }

}
