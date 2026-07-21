using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Respiral.Magic.Spell;

/// <summary>
/// Завершение сосредоточения заклинания «истинное имя». Поднимается на цели (по её
/// <c>IdentityComponent</c>); сервер на успехе записывает истинное имя цели в заметки кастера.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class RevealTrueNameDoAfterEvent : SimpleDoAfterEvent
{
}
