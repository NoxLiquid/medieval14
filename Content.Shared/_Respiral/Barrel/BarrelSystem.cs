using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Storage.Components;

namespace Content.Shared._Respiral.Barrel;

public sealed partial class BarrelSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BarrelComponent, SolutionTransferAttemptEvent>(OnTransferAttempt);
    }

    private void OnTransferAttempt(Entity<BarrelComponent> ent, ref SolutionTransferAttemptEvent args)
    {
        if (TryComp<EntityStorageComponent>(ent, out var storage) && !storage.Open)
            args.Cancel(Loc.GetString("barrel-closed-no-transfer", ("barrel", ent.Owner)));
    }
}
