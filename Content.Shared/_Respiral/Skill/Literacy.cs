using Content.Shared._Respiral.Skill.Components;
using Content.Shared._Respiral.Skill.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Respiral.Skill;

public static class Literacy
{
    public static readonly ProtoId<SkillPrototype> Skill = "MedievalLiteracy";

    /// <summary>
    ///     Возвращает уровень грамотности сущности (0..1). Нет компонента навыков — сущность
    ///     считается полностью грамотной
    /// </summary>
    public static float GetLevel(EntityUid uid, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent<SkillComponent>(uid, out var skill))
            return 1f;

        return skill.Skills.TryGetValue(Skill, out var progress) ? progress : 0f;
    }
}
