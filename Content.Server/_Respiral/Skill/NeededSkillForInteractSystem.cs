using Content.Server._Respiral.Skill.Components;
using Content.Shared._Respiral.Skill.Events;
using Content.Shared.Examine;
using Content.Shared.Interaction;

namespace Content.Server._Respiral.Skill;

public sealed partial class NeededSkillForInteractSystem : EntitySystem
{
    [Dependency] private SkillSystem _skillSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NeededSkillForInteractComponent, BeforeInteractionActivate>(OnBeforeActivate);
        SubscribeLocalEvent<NeededSkillForInteractComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<NeededSkillForInteractComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, NeededSkillForInteractComponent component, ExaminedEvent args)
    {
        if (component.NeededSkills.Count == 0)
            return;

        args.PushMarkup(Loc.GetString("skill-exm-needed-skills",
            ("skills", _skillSystem.GetSkillNames(component.NeededSkills))));
    }

    private void OnBeforeActivate(EntityUid uid, NeededSkillForInteractComponent component, BeforeInteractionActivate args)
    {
        if (args.Handled)
            return;

        if (!_skillSystem.CheckRequiredSkills(args.User, component.NeededSkills))
            args.Handled = true;
    }

    private void OnInteractHand(EntityUid uid, NeededSkillForInteractComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!_skillSystem.CheckRequiredSkills(args.User, component.NeededSkills))
            args.Handled = true;
    }

}
