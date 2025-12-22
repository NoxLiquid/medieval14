using System.Linq;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Shared.Durability;

public abstract partial class SharedDurabilitySystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DurabilityComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<DurabilityComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.PartsContainer = _container.EnsureContainer<Container>(ent, DurabilityComponent.PartsContainerName);
        if (TryComp<PreassembledComponent>(ent, out var preassembled))
        {
            foreach (var (_, possibleParts) in preassembled.Parts)
            {
                if (possibleParts == null || possibleParts.Count == 0)
                    continue;

                var partEntity = Spawn(_random.Pick(possibleParts));
                _container.Insert(partEntity, ent.Comp.PartsContainer);

                if (preassembled.Worned && TryComp<DurabilityComponent>(partEntity, out var partDurability))
                {
                    var randomMultiplier = _random.NextFloat(preassembled.WornedGap.Min, preassembled.WornedGap.Max);
                    partDurability.CurrentDurability = Math.Max(partDurability.MaxDurability * 0.01f,
                        partDurability.CurrentDurability *= randomMultiplier);
                }
            }

            UpdateCompositeDurability(ent, ent.Comp);
        }
    }

    public bool ReduceDurability(EntityUid uid, float baseAmount, EntityUid? user = null, DurabilityComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.UsePartsDurability && component.PartsContainer != null)
            return ReducePartsDurability(uid, user, baseAmount, component);

        if (!_random.Prob(component.DamageChance))
            return false;

        var randomMultiplier = GetRandomDurabilityLoss(component);
        var actualLoss = baseAmount * randomMultiplier;

        component.CurrentDurability = Math.Max(0f, component.CurrentDurability - actualLoss);

        if (component.CurrentDurability <= 0f)
        {
            HandleItemBreak(uid, user, component);
            return true;
        }

        return false;
    }

    private bool ReducePartsDurability(EntityUid uid, EntityUid? user, float baseAmount, DurabilityComponent component)
    {
        if (component.PartsContainer == null || component.PartsContainer.Count == 0)
            return false;

        var parts = component.PartsContainer.ContainedEntities.ToList();
        if (parts.Count == 0)
            return false;

        var targetPart = _random.Pick(parts);
        if (TryComp<DurabilityComponent>(targetPart, out var durability)
            && TryComp<PartDurabilityComponent>(targetPart, out var partDurability))
        {
            if (!_random.Prob(partDurability.PartDamageChance))
                return false;

            var partDamage = baseAmount * partDurability.PartDamageMultiplier *
                GetRandomDurabilityLoss(component);

            durability.CurrentDurability = Math.Max(0f, durability.CurrentDurability - partDamage);

            if (durability.CurrentDurability <= 0f)
            {
                HandlePartBreak(uid, targetPart, user, component, durability);

                if (CheckIfItemShouldBreak(uid, component, partDurability))
                {
                    HandleItemBreak(uid, user, component);
                    return true;
                }
            }

            UpdateCompositeDurability(uid, component);
            return false;
        }

        return false;
    }

    public void RepairDurability(EntityUid uid, float amount, DurabilityComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.CurrentDurability = Math.Min(component.MaxDurability, component.CurrentDurability + amount);
    }

    public void FullyRepair(EntityUid uid, DurabilityComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.CurrentDurability = component.MaxDurability;
    }

    private void HandleItemBreak(EntityUid uid, EntityUid? user, DurabilityComponent component)
    {
        if (component.BreakSound != null)
            _audio.PlayPredicted(component.BreakSound, uid, uid);

        if (component.UsePartsDurability && component.PartsContainer != null
            && component.PartsContainer.Count > 0)
        {
            _container.EmptyContainer(component.PartsContainer, true);
            QueueDel(uid);
        }
        else if (component.DestroyOnBreak)
        {
            if (component.DestroyObjects != null)
            {
                bool inHand = false;
                var coords = Transform(uid).Coordinates;
                foreach (var objectItem in component.DestroyObjects)
                {
                    var item = Spawn(objectItem, coords);
                    if (component.LeaveInHands && !inHand && user != null)
                    {
                        if (TryComp<PartDurabilityComponent>(item, out var partDurability) && partDurability.CanPickable)
                            inHand = _hands.TryForcePickupAnyHand(user.Value, item);
                    }
                }
            }

            QueueDel(uid);
        }

        var breakEvent = new ItemBrokenEvent(uid);
        RaiseLocalEvent(uid, ref breakEvent);
    }

    private void HandlePartBreak(EntityUid itemUid, EntityUid partUid, EntityUid? user, DurabilityComponent itemDurability, DurabilityComponent partDurability)
    {
        if (itemDurability.PartsContainer != null)
            _container.Remove(partUid, itemDurability.PartsContainer);

        if (partDurability.BreakSound != null)
            _audio.PlayPredicted(partDurability.BreakSound, itemUid, itemUid);

        if (partDurability.DestroyOnBreak)
        {
            if (partDurability.DestroyObjects != null)
            {
                var coords = Transform(itemUid).Coordinates;
                foreach (var objectItem in partDurability.DestroyObjects)
                {
                    Spawn(objectItem, coords);
                }
            }
            QueueDel(partUid);
        }

        var breakEvent = new ItemPartBrokenEvent(itemUid, partUid);
        RaiseLocalEvent(itemUid, ref breakEvent);
    }

    private bool CheckIfItemShouldBreak(EntityUid uid, DurabilityComponent component, PartDurabilityComponent part)
    {
        if (component.PartsContainer == null || component.PartsContainer.Count == 0)
            return true;

        if (part.IsCriticalPart)
            return true;

        return false;
    }

    private float GetRandomDurabilityLoss(DurabilityComponent component)
        => _random.NextFloat(component.MinLossMultiplier, component.MaxLossMultiplier);

    public float GetDurabilityPercent(EntityUid uid, DurabilityComponent? component = null)
    {
        if (!Resolve(uid, ref component) || component.MaxDurability <= 0f)
            return 0f;

        return component.CurrentDurability / component.MaxDurability;
    }

    private void UpdateCompositeDurability(EntityUid uid, DurabilityComponent component)
    {
        if (!component.UsePartsDurability || component.PartsContainer == null)
            return;

        float totalDurability = 0;
        float totalMaxDurability = 0;

        foreach (var part in component.PartsContainer.ContainedEntities)
        {
            if (TryComp<DurabilityComponent>(part, out var partDurability))
            {
                totalDurability += partDurability.CurrentDurability;
                totalMaxDurability += partDurability.MaxDurability;
            }
        }

        component.CurrentDurability = totalDurability;
        component.MaxDurability = totalMaxDurability;
    }

    public float GetCompositeDurabilityPercent(EntityUid uid, DurabilityComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return 0f;

        if (!component.UsePartsDurability || component.PartsContainer == null || component.PartsContainer.Count == 0)
            return GetDurabilityPercent(uid, component);

        if (component.MaxDurability <= 0f)
            return 0f;

        return component.CurrentDurability / component.MaxDurability;
    }
}

[ByRefEvent]
public readonly struct ItemBrokenEvent
{
    public readonly EntityUid Item;

    public ItemBrokenEvent(EntityUid item)
    {
        Item = item;
    }
}

[ByRefEvent]
public readonly struct ItemPartBrokenEvent
{
    public readonly EntityUid Item;
    public readonly EntityUid Part;

    public ItemPartBrokenEvent(EntityUid item, EntityUid part)
    {
        Item = item;
        Part = part;
    }
}
