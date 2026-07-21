using Robust.Shared.Serialization;

namespace Content.Shared._Respiral.Identity;

/// <summary>
/// Ключ внешнего вида грамоты: развёрнута ли она (режим «предъявить всем»).
/// </summary>
[Serializable, NetSerializable]
public enum MedievalCredentialVisuals : byte
{
    Presented,
}
