using Content.Shared._Respiral.Lock.Components;
using Content.Shared.Doors;
using Content.Shared.Storage.Components;
using Robust.Shared.Containers;

namespace Content.Shared._Respiral.Lock;

public abstract partial class SharedMedievalLockSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedievalLockableComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
        SubscribeLocalEvent<MedievalLockableComponent, StorageOpenAttemptEvent>(OnStorageOpenAttempt);
    }

    private void OnBeforeDoorOpened(EntityUid uid, MedievalLockableComponent component, BeforeDoorOpenedEvent args)
    {
        if (!CanUnlock(uid, out _))
            args.Cancel();
    }

    private void OnStorageOpenAttempt(EntityUid uid, MedievalLockableComponent component, ref StorageOpenAttemptEvent args)
    {
        if (!CanUnlock(uid, out _))
            args.Cancelled = true;
    }

    private bool CanUnlock(EntityUid uid, out MedievalLockComponent? lockComp)
    {
        lockComp = null;

        if (TryComp<MedievalLockComponent>(uid, out lockComp))
            return !lockComp.Locked;

        var lockEnt = GetLock(uid);
        if (lockEnt != null && TryComp<MedievalLockComponent>(lockEnt, out lockComp))
            return !lockComp.Locked;

        return true;
    }

    public bool IsLocked(EntityUid uid, MedievalLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        return component.Locked;
    }

    public bool TryUnlock(EntityUid uid, string key, MedievalLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (String.IsNullOrEmpty(component.KeyId))
            return false;

        if (component.KeyId != key)
            return false;

        if (!component.Locked)
            return false;

        UnLock(uid, component);

        return true;
    }

    public bool TryLock(EntityUid uid, string key, MedievalLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (String.IsNullOrEmpty(component.KeyId))
            return false;

        if (component.KeyId != key)
            return false;

        if (component.Locked)
            return false;

        Lock(uid, component);

        return true;
    }

    public void Lock(EntityUid uid, MedievalLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Locked = true;
        Dirty(uid, component);
    }

    public void UnLock(EntityUid uid, MedievalLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Locked = false;
        Dirty(uid, component);
    }

    public void SetKey(EntityUid uid, string key, MedievalLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.KeyId = key;
        Dirty(uid, component);
    }

    public EntityUid? GetLock(EntityUid target)
    {
        if (!_container.TryGetContainer(target, "lock-container", out var lockContainer))
            return null;

        if (lockContainer is not ContainerSlot slot)
            return null;

        return slot.ContainedEntity;
    }
}
