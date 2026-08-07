using Content.Server._Respiral.Skill;
using Content.Shared.Research.Prototypes;

namespace Content.Server.Lathe;

// Respiral - навыковый гейт производства станка. Логика вынесена в _Respiral, апстрим-файл
// LatheSystem.cs лишь вызывает CheckRecipeSkill в OnLatheQueueRecipeMessage.
public sealed partial class LatheSystem
{
    [Dependency] private SkillSystem _skill = default!;

    /// <summary>
    ///     Хватает ли навыков у <paramref name="user"/> для производства <paramref name="recipe"/>.
    ///     Показывает popup при нехватке (через <c>SkillSystem.CheckRequiredSkillsProgress</c>).
    ///     Рецепт без требований или пользователь без <c>SkillComponent</c> (NPC) — разрешено.
    /// </summary>
    private bool CheckRecipeSkill(EntityUid user, LatheRecipePrototype recipe)
    {
        if (recipe.NeededSkills is not { Count: > 0 } needed)
            return true;

        return _skill.CheckRequiredSkillsProgress(user, needed);
    }
}
