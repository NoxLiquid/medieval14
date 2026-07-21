using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Respiral.Identity.Events;

/// <summary>
/// Применение паспорта на человека. По завершении: если паспорт не заверён —
/// фиксирует цель заверения, иначе — предъявляет паспорт этому человеку.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class MedievalCredentialDoAfterEvent : SimpleDoAfterEvent
{
}
