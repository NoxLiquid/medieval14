using Content.Shared.Actions;

namespace Content.Shared._Respiral.Magic.Spell;

/// <summary>
/// Заклинание «истинное имя» (форма «ритуал по цели»): нацелено на человека и через DoAfter
/// раскрывает кастеру истинное имя цели, обходя аноним и ложный паспорт.
/// Эффект пишется в <c>KnownFacesComponent</c> кастера (см. <c>_Respiral/Identity</c>).
/// </summary>
public sealed partial class RevealTrueNameSpellEvent : EntityTargetActionEvent
{
    /// <summary>
    /// Сколько длится сосредоточение (DoAfter) перед раскрытием имени.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(5);
}
