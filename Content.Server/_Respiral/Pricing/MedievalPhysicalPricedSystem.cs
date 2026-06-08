using Content.Server.Cargo.Systems;
using Content.Server.Destructible;
using Content.Server._Respiral.Pricing.Components;
using Content.Shared.Cargo;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;

namespace Content.Server._Respiral.Trade;

public sealed class MedievalPhysicalPricedSystem : EntitySystem
{
    [Dependency] private PricingSystem _pricing = default!;
    [Dependency] private DestructibleSystem _destructible = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedievalPhysicalPricedComponent, MapInitEvent>(OnMapInit, after: [typeof(PricingSystem)]);
        SubscribeLocalEvent<MedievalPhysicalPricedComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<MedievalPhysicalPricedComponent, PriceCalculationEvent>(OnPriceCalculation);
    }

    private void OnMapInit(Entity<MedievalPhysicalPricedComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.BasePrice = _pricing.GetPrice(ent.Owner);
        ent.Comp.AddedValue = ent.Comp.BasePrice * ent.Comp.Markup;
    }

    private void OnDamageChanged(Entity<MedievalPhysicalPricedComponent> ent, ref DamageChangedEvent args)
    {
        if (!TryComp<DamageableComponent>(ent, out var damageComp))
            return;

        var maxHealth = _destructible.DestroyedAt(ent);
        if (maxHealth <= 0)
            return;

        var damageFraction = (double)damageComp.TotalDamage / (double)maxHealth;

        ent.Comp.AddedValue = ent.Comp.BasePrice * ent.Comp.Markup - ent.Comp.BasePrice * damageFraction;
    }

    private void OnPriceCalculation(Entity<MedievalPhysicalPricedComponent> ent, ref PriceCalculationEvent args)
    {
        args.Price += ent.Comp.AddedValue;
    }
}
