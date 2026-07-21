using Content.Server.Cargo.Systems;
using Content.Server.Destructible;
using Content.Server._Respiral.Pricing;
using Content.Server._Respiral.Pricing.Components;
using Content.Shared.Cargo;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;

namespace Content.Server._Respiral.Trade;

public sealed partial class MedievalPhysicalPricedSystem : EntitySystem
{
    [Dependency] private PricingSystem _pricing = default!;
    [Dependency] private DestructibleSystem _destructible = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedievalPhysicalPricedComponent, MapInitEvent>(OnMapInit, after: [typeof(PricingSystem)]);
        SubscribeLocalEvent<MedievalPhysicalPricedComponent, DamageDealtEvent>(OnDamageChanged);
        SubscribeLocalEvent<MedievalPhysicalPricedComponent, PriceCalculationEvent>(OnPriceCalculation);
        SubscribeLocalEvent<MedievalPhysicalPricedComponent, GetPricedByCompositionEvent>(OnGetPricedByComposition);
        SubscribeLocalEvent<GetEstimatedPricedByCompositionEvent>(OnGetEstimatedPricedByComposition);
    }

    // Разрешаем расчёт цены по составу для сущности с маркером
    private void OnGetPricedByComposition(Entity<MedievalPhysicalPricedComponent> ent, ref GetPricedByCompositionEvent args)
    {
        args.ByComposition = true;
    }

    // То же для прототипа с маркером
    private void OnGetEstimatedPricedByComposition(ref GetEstimatedPricedByCompositionEvent args)
    {
        if (args.Prototype.Components.ContainsKey(Factory.GetComponentName<MedievalPhysicalPricedComponent>()))
            args.ByComposition = true;
    }

    private void OnMapInit(Entity<MedievalPhysicalPricedComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.BasePrice = _pricing.GetPrice(ent.Owner);
        ent.Comp.AddedValue = ent.Comp.BasePrice * ent.Comp.Markup;
    }

    private void OnDamageChanged(Entity<MedievalPhysicalPricedComponent> ent, ref DamageDealtEvent args)
    {
        if (!TryComp<DamageableComponent>(ent, out var damageComp))
            return;

        var maxHealth = _destructible.DestroyedAt(ent);
        if (maxHealth <= 0)
            return;

        // GetTotalDamage помечен Obsolete (движок отходит от численной модели урона), но цена
        // средневекового предмета по задумке зависит от доли урона, а функциональной замены нет.
#pragma warning disable CS0618
        var damageFraction = (double)_damageable.GetTotalDamage((ent.Owner, damageComp)) / (double)maxHealth;
#pragma warning restore CS0618

        ent.Comp.AddedValue = ent.Comp.BasePrice * ent.Comp.Markup - ent.Comp.BasePrice * damageFraction;
    }

    private void OnPriceCalculation(Entity<MedievalPhysicalPricedComponent> ent, ref PriceCalculationEvent args)
    {
        args.Price += ent.Comp.AddedValue;
    }
}
