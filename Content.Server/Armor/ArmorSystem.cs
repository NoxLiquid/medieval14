using Content.Shared.Armor;
using Content.Shared.Cargo;
using Robust.Shared.Prototypes;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Materials; // _Respiral-Add

namespace Content.Server.Armor;

/// <inheritdoc/>
public sealed class ArmorSystem : SharedArmorSystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArmorComponent, PriceCalculationEvent>(GetArmorPrice);
    }

    private void GetArmorPrice(EntityUid uid, ArmorComponent component, ref PriceCalculationEvent args)
    {
        // _Respiral-start: если есть PhysicalComposition, то цена считается через материалы, иначе через коэффициенты брони
        if (TryComp<PhysicalCompositionComponent>(uid, out _))
            return;
        // _Respiral-end

        foreach (var modifier in component.Modifiers.Coefficients)
        {
            var damageType = _protoManager.Index<DamageTypePrototype>(modifier.Key);
            args.Price += component.PriceMultiplier * damageType.ArmorPriceCoefficient * 100 * (1 - modifier.Value);
        }

        foreach (var modifier in component.Modifiers.FlatReduction)
        {
            var damageType = _protoManager.Index<DamageTypePrototype>(modifier.Key);
            args.Price += component.PriceMultiplier * damageType.ArmorPriceFlat * modifier.Value;
        }
    }
}
