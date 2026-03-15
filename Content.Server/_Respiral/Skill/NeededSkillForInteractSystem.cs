using Content.Server._Respiral.Skill.Components;
using Content.Shared._Respiral.Skills.Events;
using Content.Shared.Interaction;

namespace Content.Server._Respiral.Skill;

public sealed class NeededSkillForInteractSystem : EntitySystem
{
    [Dependency] private readonly SkillSystem _skillSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NeededSkillForInteractComponent, BeforeInteractionActivate>(OnBeforeActivate);
        SubscribeLocalEvent<NeededSkillForInteractComponent, InteractHandEvent>(OnInteractHand);
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
