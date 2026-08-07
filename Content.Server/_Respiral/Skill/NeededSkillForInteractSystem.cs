using Content.Server._Respiral.Skill.Components;
using Content.Shared._Respiral.Skill.Events;
using Content.Shared._Respiral.Skill.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Robust.Shared.Prototypes;

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
        if (component.NeededSkillsProgress.Count == 0)
            return;

        var skills = new List<ProtoId<SkillPrototype>>(component.NeededSkillsProgress.Keys);

        args.PushMarkup(Loc.GetString("skill-exm-needed-skills",
            ("skills", _skillSystem.GetSkillNames(skills))));
    }

    private void OnBeforeActivate(EntityUid uid, NeededSkillForInteractComponent component, BeforeInteractionActivate args)
    {
        if (args.Handled)
            return;

        if (!_skillSystem.CheckRequiredSkillsProgress(args.User, component.NeededSkillsProgress))
            args.Handled = true;
    }

    private void OnInteractHand(EntityUid uid, NeededSkillForInteractComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!_skillSystem.CheckRequiredSkillsProgress(args.User, component.NeededSkillsProgress))
            args.Handled = true;
    }

}
