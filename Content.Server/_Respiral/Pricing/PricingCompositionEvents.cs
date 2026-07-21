using Robust.Shared.Prototypes;

namespace Content.Server._Respiral.Pricing;

// Точки расширения: считать ли цену по PhysicalComposition без MaterialComponent.

/// <summary>Поднимается на сущности: подписчик может разрешить расчёт цены по составу.</summary>
[ByRefEvent]
public record struct GetPricedByCompositionEvent
{
    public bool ByComposition = false;

    public GetPricedByCompositionEvent() { }
}

/// <summary>То же для прототипа (оценочная цена), поднимается широковещательно.</summary>
[ByRefEvent]
public record struct GetEstimatedPricedByCompositionEvent(EntityPrototype Prototype)
{
    public bool ByComposition = false;
}
