using Robust.Shared.Localization;

namespace Content.Shared._Respiral.Skills;

/// <summary>
///     Тип (уровень) знания навыка, зависящий от прогресса освоения 0..1.
///     Используется для показа знаний при демонстрации и как порог требований в системах NeedSkill.
/// </summary>
public enum SkillKnowledgeLevel : byte
{
    /// <summary>Слабое представление, прогресс &lt; 0.2.</summary>
    Vague = 0,

    /// <summary>Есть понимание, прогресс &lt; 0.4.</summary>
    Basic,

    /// <summary>Понимание, прогресс &lt; 0.6.</summary>
    Understanding,

    /// <summary>Уверенное владение, прогресс &lt; 0.8.</summary>
    Confident,

    /// <summary>Глубокие знания, прогресс &lt; 1.0.</summary>
    Deep,

    /// <summary>Мастерство, прогресс &gt;= 1.0.</summary>
    Mastery,
}

/// <summary>
///     Помощник для перевода прогресса освоения навыка (0..1) в <see cref="SkillKnowledgeLevel"/>
///     и обратно, а также в локализованное название.
/// </summary>
public static class SkillKnowledge
{
    /// <summary>Уровень знания по прогрессу освоения 0..1.</summary>
    public static SkillKnowledgeLevel GetLevel(float progress) => progress switch
    {
        < 0.2f => SkillKnowledgeLevel.Vague,
        < 0.4f => SkillKnowledgeLevel.Basic,
        < 0.6f => SkillKnowledgeLevel.Understanding,
        < 0.8f => SkillKnowledgeLevel.Confident,
        < 1f => SkillKnowledgeLevel.Deep,
        _ => SkillKnowledgeLevel.Mastery,
    };

    /// <summary>Минимальный прогресс (0..1), с которого начинается уровень знания.</summary>
    public static float GetMinProgress(SkillKnowledgeLevel level) => level switch
    {
        SkillKnowledgeLevel.Basic => 0.2f,
        SkillKnowledgeLevel.Understanding => 0.4f,
        SkillKnowledgeLevel.Confident => 0.6f,
        SkillKnowledgeLevel.Deep => 0.8f,
        SkillKnowledgeLevel.Mastery => 1f,
        _ => 0f,
    };

    /// <summary>Локализованное название уровня знания.</summary>
    public static string GetLevelName(SkillKnowledgeLevel level) => Loc.GetString($"skill-knowledge-{LevelKey(level)}");

    /// <summary>Локализованное название уровня знания по прогрессу освоения 0..1.</summary>
    public static string GetLevelName(float progress) => GetLevelName(GetLevel(progress));

    private static string LevelKey(SkillKnowledgeLevel level) => level switch
    {
        SkillKnowledgeLevel.Vague => "vague",
        SkillKnowledgeLevel.Basic => "basic",
        SkillKnowledgeLevel.Understanding => "understanding",
        SkillKnowledgeLevel.Confident => "confident",
        SkillKnowledgeLevel.Deep => "deep",
        SkillKnowledgeLevel.Mastery => "mastery",
        _ => "vague",
    };
}
