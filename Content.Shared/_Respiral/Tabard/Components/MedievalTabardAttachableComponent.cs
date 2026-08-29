using Robust.Shared.GameStates;

namespace Content.Shared._Respiral.Tabard.Components;

/// <summary>
/// Флаг: к этой одежде можно прикрепить <see cref="MedievalTabardComponent"/> (в основном броня).
/// Прикреплённый табард хранится в контейнере <see cref="ContainerId"/>; кэш его отрисовки
/// сетевой, чтобы клиент рисовал накладку без доступа к содержимому контейнера.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class MedievalTabardAttachableComponent : Component
{
    public const string ContainerId = "tabard-slot";

    /// <summary>
    /// Путь к RSI прикреплённого табарда. null = табард не прикреплён.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public string? RsiPath;

    /// <summary>
    /// Состояние наложения на экипированную одежду.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public string? EquippedState;

    /// <summary>
    /// Состояние наложения на иконку предмета.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public string? IconState;

    /// <summary>
    /// Аффилиация дома (LocId) от прикреплённого табарда — для системы идентификации. null = нет.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public string? Affiliation;
}
