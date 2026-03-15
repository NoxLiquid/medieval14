using Content.Shared._Respiral.Skills.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Respiral.Skill.Components;

[RegisterComponent]
public sealed partial class MeleeSkillComponent : Component
{
    /// <summary>
    ///     Множитель без какого-либо навыка
    /// </summary>
    [DataField]
    public float DefaultModifier = 2f;

    /// <summary>
    ///     Требуемые навыки
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<SkillPrototype>> Skills;

    /// <summary>
    ///     Множители урона на навык (будет браться максимальный из всех)
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<SkillPrototype>, float> DamageModifiers { get; set; } = new Dictionary<ProtoId<SkillPrototype>, float>();

}
