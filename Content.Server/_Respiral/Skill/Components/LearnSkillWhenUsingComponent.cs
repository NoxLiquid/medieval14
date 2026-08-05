using Content.Shared._Respiral.Skills;
using Content.Shared._Respiral.Skills.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Respiral.Skill.Components;

[RegisterComponent]
public sealed partial class LearnSkillWhenUsingComponent : Component
{
    /// <summary>
    ///     Изучаемые навыки: id, до какого уровня можно поднять, очки за использование,
    ///     минимальный текущий уровень для начала изучения по этой записи.
    /// </summary>
    [DataField(required: true)]
    public List<LearnSkillEntry> Skills = new();

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

/// <summary>
///     Одна запись изучаемого навыка: за что и в каких пределах учит этот предмет.
/// </summary>
[DataDefinition]
public sealed partial class LearnSkillEntry
{
    /// <summary>
    ///     Изучаемый навык
    /// </summary>
    [DataField(required: true)]
    public ProtoId<SkillPrototype> Skill;

    /// <summary>
    ///     Уровень знания, выше которого эта запись навык не поднимает
    /// </summary>
    [DataField]
    public SkillKnowledgeLevel MaxLevel = SkillKnowledgeLevel.Mastery;

    /// <summary>
    ///     Очки прогресса за одно успешное изучение
    /// </summary>
    [DataField(required: true)]
    public float Points;

    /// <summary>
    ///     Минимальный текущий уровень знания навыка, необходимый, чтобы начать изучение по этой записи
    /// </summary>
    [DataField]
    public SkillKnowledgeLevel RequiredLevel = SkillKnowledgeLevel.Vague;
}
