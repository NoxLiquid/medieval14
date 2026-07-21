using Robust.Shared.Prototypes;

namespace Content.Shared._Respiral.Skill.Prototypes;

[Prototype]
public sealed partial class SkillGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name = string.Empty;

    [DataField(required: true)]
    public List<ProtoId<SkillPrototype>> Skills = new();

}
