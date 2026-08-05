using System.Collections.Generic;
using Content.Shared._Respiral.Skills;
using Content.Shared.Research.Prototypes;
using Robust.Client.Player;

namespace Content.Client.Lathe.UI;

// Respiral - навыковый гейт рецептов в UI станка. Логика в _Respiral, апстрим-файл LatheMenu.xaml.cs
// лишь вызывает MeetsRecipeSkill (для недоступности) и GetRecipeSkillTooltip (для подсказки).
public sealed partial class LatheMenu
{
    [Dependency] private IPlayerManager _playerManager = default!;

    /// <summary>
    ///     Хватает ли локальному игроку навыков для этого рецепта (нет требований — да).
    /// </summary>
    private bool MeetsRecipeSkill(LatheRecipePrototype recipe)
    {
        if (_playerManager.LocalEntity is not { } player)
            return true;

        return SkillRequirement.Meets(player, recipe.NeededSkills, _entityManager);
    }

    /// <summary>
    ///     Строка подсказки с требуемыми навыками и уровнями, если их не хватает; иначе null.
    /// </summary>
    private string? GetRecipeSkillTooltip(LatheRecipePrototype recipe)
    {
        if (recipe.NeededSkills is not { Count: > 0 } needed || MeetsRecipeSkill(recipe))
            return null;

        var parts = new List<string>();
        foreach (var (id, level) in needed)
        {
            if (!_prototypeManager.Resolve(id, out var proto))
                continue;

            parts.Add(Loc.GetString("skill-lathe-needed-entry",
                ("skill", proto.Name),
                ("level", SkillKnowledge.GetLevelName(level))));
        }

        if (parts.Count == 0)
            return null;

        return Loc.GetString("skill-lathe-needed", ("skills", string.Join(", ", parts)));
    }
}
