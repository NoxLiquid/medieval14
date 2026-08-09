using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Pulling.Events;

namespace Content.Shared._Respiral.Rope;

public sealed class SharedRopeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RopedComponent, AttemptStopPullingEvent>(OnAttemptStopPulling);
    }

    private void OnAttemptStopPulling(Entity<RopedComponent> ent, ref AttemptStopPullingEvent args)
    {
        if (TryComp<PullableComponent>(ent.Owner, out var pullable) && pullable.Puller == ent.Comp.Partner)
            args.Cancelled = true;
    }
}
