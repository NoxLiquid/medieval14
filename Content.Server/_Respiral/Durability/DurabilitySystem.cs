using Content.Shared.Durability;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Tools.Components;
using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.Tools.Systems;
using Content.Shared.Throwing;

namespace Content.Server.Durability;

public sealed partial class DurabilitySystem : SharedDurabilitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        BeforeInitialize();

        SubscribeLocalEvent<DurabilityComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<DurabilityComponent, ThrowDoHitEvent>(OnThrowHit);
        SubscribeLocalEvent<DurabilityComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnDamageArmorModify);
        SubscribeLocalEvent<DurabilityComponent, ToolUsingFinishedEvent>(OnToolUsingFinished);
        SubscribeLocalEvent<DurabilityComponent, SimpleToolDoAfterEvent>(OnSimpleToolDoAfter);
    }

    private void OnMeleeHit(Entity<DurabilityComponent> ent, ref MeleeHitEvent args)
    {
        if (!ent.Comp.DegradeOnUse)
            return;

        ReduceDurability(ent, ent.Comp.CurrentDurabilityLoss, args.User);
    }

    private void OnThrowHit(Entity<DurabilityComponent> ent, ref ThrowDoHitEvent args)
    {
        if (!ent.Comp.DegradeOnUse)
            return;

        ReduceDurability(ent, ent.Comp.CurrentDurabilityLoss);
    }

    private void OnDamageArmorModify(Entity<DurabilityComponent> ent, ref InventoryRelayedEvent<DamageModifyEvent> args)
    {
        if (!ent.Comp.DegradeOnUse || args.Args.OriginalDamage.GetTotal() <= 0)
            return;

        if (ShouldSkipDurabilityLoss(args.Args.OriginalDamage))
            return;

        var totalDamage = (float)args.Args.OriginalDamage.GetTotal();
        var durabilityLoss = CalculateDurabilityLoss(ent.Comp, totalDamage);

        ReduceDurability(ent, durabilityLoss);
    }

    private void OnToolUsingFinished(Entity<DurabilityComponent> ent, ref ToolUsingFinishedEvent args)
    {
        if (!ent.Comp.DegradeOnUse)
            return;

        ReduceDurability(ent, ent.Comp.CurrentDurabilityLoss);
    }

    private void OnSimpleToolDoAfter(Entity<DurabilityComponent> ent, ref SimpleToolDoAfterEvent args)
    {
        if (!ent.Comp.DegradeOnUse || args.Cancelled)
            return;

        ReduceDurability(ent, ent.Comp.CurrentDurabilityLoss, args.User);
    }

    private bool ShouldSkipDurabilityLoss(DamageSpecifier damage)
    {
        if (damage.DamageDict.ContainsKey("Asphyxiation") || damage.DamageDict.ContainsKey("Bloodloss"))
            return true;

        return false;
    }

    private float CalculateDurabilityLoss(DurabilityComponent comp, float totalDamage)
    {
        var lossFromDamage = totalDamage * comp.DamageToDurabilityRatio;
        var totalLoss = Math.Max(comp.CurrentDurabilityLoss, lossFromDamage);

        return totalLoss;
    }
}
