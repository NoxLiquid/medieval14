using Robust.Shared.GameObjects;

namespace Content.Shared._Respiral.Pricing;

/// <summary>
/// Маркерный компонент: цена сущности рассчитывается через PhysicalCompositionComponent,
/// без требования MaterialComponent.
/// </summary>
[RegisterComponent]
public sealed partial class MedievalPhysicalPricedComponent : Component
{
}
