using Robust.Shared.GameStates;

namespace Content.Shared._Respiral.Respawn;

/// <summary>
/// На сущности-призраке: момент времени, когда игрок сможет возродиться новым персонажем.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MedievalGhostRespawnComponent : Component
{
    /// <summary>
    /// Время (CurTime), начиная с которого доступен респавн. До него кнопка красная (отсчёт),
    /// после — зелёная.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan RespawnAt;
}
