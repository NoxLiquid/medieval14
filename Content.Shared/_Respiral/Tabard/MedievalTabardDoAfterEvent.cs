using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Respiral.Tabard;

/// <summary>
/// Завершение прикрепления табарда к одежде.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class MedievalTabardDoAfterEvent : SimpleDoAfterEvent
{
}
