using Content.Shared._Respiral.Skill;
using Content.Shared._Respiral.Skill.Prototypes;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server._Respiral.Skill.Components;

[RegisterComponent]
public sealed partial class NeededSkillForMeleeComponent : Component
{
    /// <summary>
    ///     Требуемые навыки владения оружием с уровнем знания, дающим полный урон.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<SkillPrototype>, SkillKnowledgeLevel> NeededSkillsProgress = new();

    /// <summary>
    ///     Количество даваемых очков при изучении
    /// </summary>
    [DataField(required: true)]
    public List<Dictionary<ProtoId<SkillPrototype>, float>> Points { get; set; } = new();

    /// <summary>
    ///     Множители очков обучения по белому списку цели.
    ///     Если цель не подходит ни под один — очки не начисляются.
    /// </summary>
    [DataField]
    public List<SkillLearnModifier> LearnModifiers { get; set; } = new();
}

[DataDefinition]
public sealed partial class SkillLearnModifier
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist = default!;

    [DataField(required: true)]
    public float Modifier = 1f;
}
