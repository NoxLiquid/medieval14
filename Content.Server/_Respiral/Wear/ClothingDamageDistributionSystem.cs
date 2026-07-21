using Content.Server._Respiral.Wear.Components;
using Content.Server.Inventory;
using Robust.Shared.Random;
using Content.Shared.Damage.Systems;

namespace Content.Server._Respiral.Wear;

public sealed partial class ClothingDamageDistributionSystem : EntitySystem
{
    [Dependency] private ServerInventorySystem _inventory = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private WearSystem _wear = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingDamageDistributionComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
    }

    private void OnBeforeDamageChanged(EntityUid uid, ClothingDamageDistributionComponent component, ref BeforeDamageChangedEvent args)
    {
        if (args.Damage == null)
            return;

        var damage = args.Damage.GetTotal().Float();
        int clothCount = (int)(damage / component.DamageThreshold);

        if (clothCount <= 0)
            return;

        // Собираем все доступные вещи с WearComponent
        var wearables = new List<(EntityUid entity, WearComponent wearComp)>();

        foreach (var slot in component.Slots)
        {
            if (_inventory.TryGetSlotContainer(uid, slot, out var containerSlot, out _)
                && containerSlot.ContainedEntity is EntityUid ent
                && TryComp<WearComponent>(ent, out var wearComp))
            {
                wearables.Add((ent, wearComp));
            }
        }

        if (wearables.Count == 0)
            return;

        // Наносим урон случайным вещам clothCount раз
        for (int i = 0; i < clothCount; i++)
        {
            var pick = _random.Next(0, wearables.Count);
            var (ent, wearComp) = wearables[pick];
            _wear.AddWear(ent, args.Damage, wearComp);
        }
    }

}
