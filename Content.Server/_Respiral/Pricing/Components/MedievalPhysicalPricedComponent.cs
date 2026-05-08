namespace Content.Server._Respiral.Pricing.Components;

// Respiral - маркер: цена сущности рассчитывается через PhysicalComposition без MaterialComponent
[RegisterComponent]
public sealed partial class MedievalPhysicalPricedComponent : Component
{
    /// <summary>
    ///     Наценка
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public double Markup = 0.2f;

    /// <summary>
    ///     Базовая (первоначальная цена)
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public double BasePrice = 0.2f;

    /// <summary>
    ///     Добавочная стоимость
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public double AddedValue = 0f;
}
