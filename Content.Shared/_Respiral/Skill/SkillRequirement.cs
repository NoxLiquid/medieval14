using Content.Shared._Respiral.Skill.Components;
using Content.Shared._Respiral.Skill.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Respiral.Skill;

/// <summary>
///     Общая проверка требований к навыкам по уровню знания (<see cref="SkillKnowledgeLevel"/>).
///     Используется там, где нужна одинаковая логика на клиенте и сервере без popup'ов
///     (например, доступность рецепта станка). Серверные гейты с popup'ом — в <c>SkillSystem</c>.
/// </summary>
public static class SkillRequirement
{
    /// <summary>
    ///     Удовлетворяет ли <paramref name="user"/> всем требованиям <paramref name="required"/>.
    ///     Пустые/отсутствующие требования — да. Нет <see cref="SkillComponent"/> (например, NPC) — тоже да
    ///     (не гейтим, как и <c>SkillSystem.CheckRequiredSkillsProgress</c>).
    /// </summary>
    public static bool Meets(
        EntityUid user,
        IReadOnlyDictionary<ProtoId<SkillPrototype>, SkillKnowledgeLevel>? required,
        IEntityManager entMan)
    {
        if (required is null || required.Count == 0)
            return true;

        if (!entMan.TryGetComponent<SkillComponent>(user, out var skill))
            return true;

        foreach (var (id, level) in required)
        {
            var progress = skill.Skills.TryGetValue(id, out var p) ? p : 0f;
            if (progress < SkillKnowledge.GetMinProgress(level))
                return false;
        }

        return true;
    }
}
