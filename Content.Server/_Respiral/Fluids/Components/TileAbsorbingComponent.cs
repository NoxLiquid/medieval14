namespace Content.Server._Respiral.Fluids.Components;

/// <summary>
/// Respiral: маркер лужи, стоящей на впитывающем тайле (<see cref="Content.Shared.Maps.ContentTileDefinition.Absorbent"/>).
/// Ставится один раз при появлении лужи, чтобы система впитывания не проверяла тайл каждый тик,
/// а сразу перебирала только релевантные лужи.
/// </summary>
[RegisterComponent]
public sealed partial class TileAbsorbingComponent : Component
{
}
