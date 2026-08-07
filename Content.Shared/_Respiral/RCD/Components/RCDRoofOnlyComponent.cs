using Robust.Shared.GameStates;

namespace Content.Shared._Respiral.RCD.Components;

/// <summary>
/// маркер на RCD-подобном инструменте (кровельный молот), ограничивающий
/// цели демонтажа только сущностями с тегом MedievalRoof. См. <see cref="RCDRoofOnlySystem"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RCDRoofOnlyComponent : Component;
