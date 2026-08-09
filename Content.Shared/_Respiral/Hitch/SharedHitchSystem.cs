using Content.Shared.DragDrop;

namespace Content.Shared._Respiral.Hitch;

public sealed class SharedHitchSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitchComponent, CanDropTargetEvent>(OnCanDropTarget);
    }

    private void OnCanDropTarget(Entity<HitchComponent> ent, ref CanDropTargetEvent args)
    {
        args.CanDrop = ent.Comp.Horse == null && HasComp<HitchableComponent>(args.Dragged);
        args.Handled = true;
    }
}
