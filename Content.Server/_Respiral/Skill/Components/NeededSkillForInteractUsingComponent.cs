using Content.Shared._Respiral.Skills;
using Content.Shared._Respiral.Skills.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Respiral.Skill.Components;

[RegisterComponent]
public sealed partial class NeededSkillForInteractUsingComponent : Component
{
    /// <summary>
    ///     Требуемые навыки с минимальным уровнем знания.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<SkillPrototype>, SkillKnowledgeLevel> NeededSkillsProgress = new();
}
