using Content.Shared._Respiral.Skill.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Respiral.Skill.Components;

[RegisterComponent]
public sealed partial class NeededSkillForInteractUsingComponent : Component
{
    /// <summary>
    ///     Требуемые навыки с минимальным прогрессом изучения (0..1).
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<SkillPrototype>, float> NeededSkillsProgress = new();
}
