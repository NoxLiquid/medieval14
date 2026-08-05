using Content.Shared._Respiral.Skills.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Respiral.Skill.Components;

[RegisterComponent]
public sealed partial class LearnSkillWhenUsingComponent : Component
{
    /// <summary>
    ///     Изучаемые навыки
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<SkillPrototype>> Skills;

    /// <summary>
    ///     Количество даваемых очков при изучении
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<SkillPrototype>, float> Points { get; set; } = new Dictionary<ProtoId<SkillPrototype>, float>();

    /// <summary>
    ///     Длительность изучения (секунд)
    /// </summary>
    [DataField]
    public float Duration = 1f;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? Sound = default!;

    // /// <summary>
    // ///     Список языков, на которых можно изучать навыки из этой книги
    // /// </summary>
    // [DataField]
    // [ViewVariables(VVAccess.ReadOnly)]
    // public List<ProtoId<LanguagePrototype>>? LanguagesWhitelist = default!;
}
