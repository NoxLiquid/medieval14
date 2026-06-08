using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Server._Respiral.Light.Components;
using Content.Shared.Interaction;
using Content.Shared.Light.Components;
using Content.Shared.Temperature;

namespace Content.Server._Respiral.Light;

public sealed partial class IgniteExpendableLightOnHotInteractSystem : EntitySystem
{
    [Dependency] private ExpendableLightSystem _expendableLight = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IgniteExpendableLightOnHotInteractComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid uid, IgniteExpendableLightOnHotInteractComponent component, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(uid, out ExpendableLightComponent? expendableLight))
            return;

        if (expendableLight.CurrentState != ExpendableLightState.BrandNew)
            return;

        var isHotEvent = new IsHotEvent();
        RaiseLocalEvent(args.Used, isHotEvent);

        if (!isHotEvent.IsHot)
            return;

        if (_expendableLight.TryActivate((uid, expendableLight)))
            args.Handled = true;
    }
}
