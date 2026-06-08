using Content.Server._Respiral.Skill.Components;
using Content.Shared._Respiral.Skills.Events;

namespace Content.Server._Respiral.Skill;

public sealed class NeededSkillForInteractUsingSystem : EntitySystem
{
    [Dependency] private SkillSystem _skillSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NeededSkillForInteractUsingComponent, BeforeInteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid uid, NeededSkillForInteractUsingComponent component, BeforeInteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!_skillSystem.CheckRequiredSkills(args.User, component.NeededSkills))
            args.Handled = true;
    }

}
