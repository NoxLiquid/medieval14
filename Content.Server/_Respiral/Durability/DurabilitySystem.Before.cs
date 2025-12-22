using Content.Shared.Durability;
using Content.Shared.Tools.Components;
using Content.Shared.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server.Durability;

public sealed partial class DurabilitySystem
{
    private void BeforeInitialize()
    {
        SubscribeLocalEvent<DurabilityComponent, AttemptMeleeEvent>(OnAttemptMelee);
        SubscribeLocalEvent<DurabilityComponent, AttemptMeleeThrowOnHitEvent>(OnAttemptMeleeThrow);
        SubscribeLocalEvent<DurabilityComponent, ToolUseAttemptEvent>(OnToolUseAttempt);
        SubscribeLocalEvent<DurabilityComponent, AttemptSimpleToolUseEvent>(OnAttemptSimpleTool);
    }

    private void OnAttemptMelee(Entity<DurabilityComponent> ent, ref AttemptMeleeEvent args)
    {
        if (ent.Comp.CurrentDurability <= 0)
            args.Cancelled = true;
    }

    private void OnAttemptMeleeThrow(Entity<DurabilityComponent> ent, ref AttemptMeleeThrowOnHitEvent args)
    {
        if (ent.Comp.CurrentDurability <= 0)
            args.Cancelled = true;
    }

    private void OnToolUseAttempt(Entity<DurabilityComponent> ent, ref ToolUseAttemptEvent args)
    {
        if (ent.Comp.CurrentDurability <= 0)
            args.Cancel();
    }

    private void OnAttemptSimpleTool(Entity<DurabilityComponent> ent, ref AttemptSimpleToolUseEvent args)
    {
        if (ent.Comp.CurrentDurability <= 0)
            args.Cancelled = true;
    }
}
