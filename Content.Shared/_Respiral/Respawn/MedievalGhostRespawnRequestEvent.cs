using Robust.Shared.Serialization;

namespace Content.Shared._Respiral.Respawn;

/// <summary>
/// Запрос игрока-призрака на респавн.
/// </summary>
[Serializable, NetSerializable]
public sealed class MedievalGhostRespawnRequestEvent : EntityEventArgs
{
}
