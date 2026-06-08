using Content.Shared._Respiral.Lock.Components;
using Content.Shared.Doors;
using Content.Shared.Storage.Components;
using Robust.Shared.Containers;

namespace Content.Shared._Respiral.Lock;

public abstract partial class SharedMedievalLockSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedievalLockableComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
        SubscribeLocalEvent<MedievalLockableComponent, StorageOpenAttemptEvent>(OnStorageOpenAttempt);
    }

    private void OnBeforeDoorOpened(EntityUid uid, MedievalLockableComponent component, BeforeDoorOpenedEvent args)
    {
        if (!CanUnlock(uid))
            args.Cancel();
    }

    private void OnStorageOpenAttempt(EntityUid uid, MedievalLockableComponent component, ref StorageOpenAttemptEvent args)
    {
        if (!CanUnlock(uid))
            args.Cancelled = true;
    }

    private bool CanUnlock(EntityUid uid)
    {
        var lockEnt = GetLock(uid);
        if (lockEnt == null)
            return true;

        if (!TryComp<MedievalLockComponent>(lockEnt, out var lockComp))
            return true;

        return !lockComp.Locked;
    }

    public bool IsLocked(EntityUid uid)
    {
        var lockEnt = GetLock(uid);
        if (lockEnt == null)
            return false;

        if (!TryComp<MedievalLockComponent>(lockEnt, out var lockComp))
            return false;

        return lockComp.Locked;
    }

    public bool IsLocked(EntityUid uid, MedievalLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        return component.Locked;
    }

    public bool TryUnlock(EntityUid uid, string key)
    {
        var lockEnt = GetLock(uid);
        if (lockEnt == null)
            return false;

        if (!TryComp<MedievalLockComponent>(lockEnt, out var lockComp))
            return false;

        if (string.IsNullOrEmpty(lockComp.KeyId) || lockComp.KeyId != key || !lockComp.Locked)
            return false;

        UnLock(lockEnt.Value, lockComp);
        return true;
    }

    public bool TryUnlock(EntityUid uid, string key, MedievalLockComponent component)
    {
        if (string.IsNullOrEmpty(component.KeyId) || component.KeyId != key || !component.Locked)
            return false;

        UnLock(uid, component);
        return true;
    }

    public bool TryLock(EntityUid uid, string key)
    {
        var lockEnt = GetLock(uid);
        if (lockEnt == null)
            return false;

        if (!TryComp<MedievalLockComponent>(lockEnt, out var lockComp))
            return false;

        if (string.IsNullOrEmpty(lockComp.KeyId) || lockComp.KeyId != key || lockComp.Locked)
            return false;

        Lock(lockEnt.Value, lockComp);
        return true;
    }

    public bool TryLock(EntityUid uid, string key, MedievalLockComponent component)
    {
        if (string.IsNullOrEmpty(component.KeyId) || component.KeyId != key || component.Locked)
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

    public EntityUid? GetLock(EntityUid target)
    {
        if (!_container.TryGetContainer(target, "lock-container", out var lockContainer))
            return null;

        if (lockContainer is not ContainerSlot slot)
            return null;

        return slot.ContainedEntity;
    }

    public void SetKey(EntityUid uid, string key, MedievalLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.KeyId = key;
    }
}
