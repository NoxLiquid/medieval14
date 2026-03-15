using Robust.Shared.Prototypes;

namespace Content.Shared._Respiral.Skills.Prototypes;

[Prototype("skillGroup")]
public sealed partial class SkillGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name = string.Empty;

    [DataField(required: true)]
    public List<ProtoId<SkillPrototype>> Skills = new();

}
