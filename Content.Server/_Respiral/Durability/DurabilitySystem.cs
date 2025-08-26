using Content.Shared.Durability;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Tools.Components;
using Content.Shared.Examine;
using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.Tools.Systems;
using Content.Shared.Throwing;

namespace Content.Server.Durability;

public sealed class DurabilitySystem : SharedDurabilitySystem
{
    public const float DamageToDurabilityRatio = 0.1f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DurabilityComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<DurabilityComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<DurabilityComponent, ThrowDoHitEvent>(OnThrowHit);
        SubscribeLocalEvent<DurabilityComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnDamageArmorModify);
        SubscribeLocalEvent<DurabilityComponent, ToolUsingFinishedEvent>(OnToolUsingFinished);
        SubscribeLocalEvent<DurabilityComponent, SimpleToolDoAfterEvent>(OnSimpleToolDoAfter);
    }

    private void OnExamined(Entity<DurabilityComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var percent = GetDurabilityPercent(ent);
        var condition = GetConditionDescription(percent);
        var color = GetConditionColor(percent);

        var message = Loc.GetString("durability-examined",
            ("name", Name(ent)),
            ("condition", condition),
            ("color", color));

        args.PushMarkup(message);
    }

    private void OnMeleeHit(Entity<DurabilityComponent> ent, ref MeleeHitEvent args)
    {
        if (!ent.Comp.DegradeOnUse)
            return;

        ReduceDurability(ent, ent.Comp.CurrentDurabilityLoss);
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

        ReduceDurability(ent, ent.Comp.CurrentDurabilityLoss);
    }

    private string GetConditionDescription(float percent)
    {
        return percent switch
        {
            >= 0.9f => Loc.GetString("durability-pristine"),
            >= 0.7f => Loc.GetString("durability-good"),
            >= 0.5f => Loc.GetString("durability-worn"),
            >= 0.3f => Loc.GetString("durability-poor"),
            >= 0.1f => Loc.GetString("durability-bad"),
            > 0f => Loc.GetString("durability-critical"),
            _ => Loc.GetString("durability-broken")
        };
    }

    private string GetConditionColor(float percent)
    {
        return percent switch
        {
            >= 0.9f => "#4CAF50",
            >= 0.7f => "#8BC34A",
            >= 0.5f => "#CDDC39",
            >= 0.3f => "#FF9800",
            >= 0.1f => "#F44339",
            > 0f => "#D32F2F",
            _ => "#795548"
        };
    }

    private bool ShouldSkipDurabilityLoss(DamageSpecifier damage)
    {
        if (damage.DamageDict.ContainsKey("Asphyxiation") || damage.DamageDict.ContainsKey("Bloodloss"))
            return true;

        return false;
    }

    private float CalculateDurabilityLoss(DurabilityComponent comp, float totalDamage)
    {
        var lossFromDamage = totalDamage * DamageToDurabilityRatio;
        var totalLoss = Math.Max(comp.CurrentDurabilityLoss, lossFromDamage);

        return totalLoss;
    }
}
