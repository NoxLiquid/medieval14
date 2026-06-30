using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared._Respiral.Magic.Spell;

/// <summary>
/// Заклинание-снаряд (форма «шар»): спавнит снаряд <see cref="Prototype"/> и пускает его
/// в сторону цели. Элемент берётся из <see cref="SpellComponent"/> сущности-действия.
/// </summary>
public sealed partial class ProjectileSpellEvent : WorldTargetActionEvent
{
    /// <summary>
    /// Какой снаряд спавнить (обычно общий ProjMagicBall — элемент решает остальное).
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype;
}
