using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared._Respiral.Magic.Spell;

public sealed partial class BallSpellEvent : WorldTargetActionEvent
{
    /// <summary>
    ///     Какой снаряд спавнить
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype;
}
