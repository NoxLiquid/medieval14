using Content.Server._Respiral.Wear.Components;
using Content.Shared.Lathe;

namespace Content.Server._Respiral.Wear;

public sealed partial class LatheWearSystem : EntitySystem
{
    [Dependency] private WearSystem _wear = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LatheWearComponent, LatheQueueRecipeMessage>(OnLatheQueueRecipeMessage);
    }

    private void OnLatheQueueRecipeMessage(EntityUid uid, LatheWearComponent component, LatheQueueRecipeMessage args)
    {
        if (!component.AddPoints.TryGetValue(args.ID, out var currentPoints))
            currentPoints = component.DefaultAddPoints;

        if (TryComp<WearComponent>(uid, out var wearComp))
            _wear.AddWear(uid, component.Damage * currentPoints * args.Quantity, wearComp);
    }

}
