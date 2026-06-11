using Content.Shared._Respiral.Lock;
using Content.Server._Respiral.Lock.Components;
using Content.Shared._Respiral.Lock.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Content.Shared.Examine;
using Robust.Shared.Containers;

namespace Content.Server._Respiral.Lock;

public sealed partial class MedievalLockSystem : SharedMedievalLockSystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private MedievalKeySystem _medievalKey = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedievalLockComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MedievalLockComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<LockExamineComponent, ExaminedEvent>(OnExamine);
    }

    private void OnMapInit(EntityUid uid, MedievalLockComponent component, MapInitEvent args)
    {
        if (!component.GenerateKey)
            return;

        component.KeyId = _medievalKey.GenerateKeyId();
    }

    private void OnExamine(EntityUid uid, LockExamineComponent component, ExaminedEvent args)
    {
        if (!_container.TryGetContainer(uid, "lock-container", out var lockContainer))
            return;

        if (lockContainer is ContainerSlot slot)
        {
            if (slot.ContainedEntity != null)
                args.PushMarkup(Loc.GetString("medieval-lock-attached"));
        }
    }

    private void OnAfterInteract(EntityUid uid, MedievalLockComponent component, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target is not { } target)
            return;

        if (!_container.TryGetContainer(target, "lock-container", out var lockContainer))
        {
            _popup.PopupEntity(Loc.GetString("medieval-add-lock-fail2"), args.User, args.User);
            return;
        }

        if (lockContainer is ContainerSlot slot)
        {
            if (slot.ContainedEntity != null)
            {
                _popup.PopupEntity(Loc.GetString("medieval-add-lock-fail1"), args.User, args.User);
                return;
            }

            if (!_container.Insert(uid, lockContainer))
            {
                _popup.PopupEntity(Loc.GetString("medieval-add-lock-fail3"), args.User, args.User);

                EnsureComp<MedievalLockableComponent>(target);

                return;
            }

            if (component.AttachSound != null)
                _audio.PlayPvs(component.AttachSound, Transform(target).Coordinates);

            _popup.PopupEntity(Loc.GetString("medieval-add-lock-pass"), args.User, args.User);
        }
    }

}
