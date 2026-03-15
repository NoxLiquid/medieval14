using Content.Shared._Respiral.Skills.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Respiral.Skill.Components;

[RegisterComponent]
public sealed partial class NeededSkillForInteractComponent : Component
{
    /// <summary>
    ///     Требуемые навыки
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<SkillPrototype>> NeededSkills;
}
