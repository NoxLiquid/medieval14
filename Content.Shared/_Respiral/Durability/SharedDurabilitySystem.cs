using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Shared.Durability;

public abstract partial class SharedDurabilitySystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public bool ReduceDurability(EntityUid uid, float baseAmount, DurabilityComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        var randomMultiplier = GetRandomDurabilityLoss(component);
        var actualLoss = baseAmount * randomMultiplier;

        component.CurrentDurability = Math.Max(0f, component.CurrentDurability - actualLoss);

        if (component.CurrentDurability <= 0f)
        {
            HandleItemBreak(uid, component);
            return true;
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

    private void HandleItemBreak(EntityUid uid, DurabilityComponent component)
    {
        if (component.BreakSound != null)
            _audio.PlayPredicted(component.BreakSound, uid, uid);

        if (component.DestroyOnBreak)
        {
            if (component.DestroyObject != null)
                Spawn(component.DestroyObject, Transform(uid).Coordinates);

            QueueDel(uid);
        }

        var breakEvent = new ItemBrokenEvent(uid);
        RaiseLocalEvent(uid, ref breakEvent);
    }

    private float GetRandomDurabilityLoss(DurabilityComponent component)
        => _random.NextFloat(component.MinLossMultiplier, component.MaxLossMultiplier);

    public float GetDurabilityPercent(EntityUid uid, DurabilityComponent? component = null)
    {
        if (!Resolve(uid, ref component) || component.MaxDurability <= 0f)
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
