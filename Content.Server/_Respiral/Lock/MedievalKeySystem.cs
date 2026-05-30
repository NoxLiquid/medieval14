using Content.Shared._Respiral.Lock;
using Content.Server._Respiral.Lock.Components;
using Content.Shared._Respiral.Lock.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Shared.Random;
using Content.Shared._Respiral.Lock.Events;
using Robust.Server.Audio;
using Content.Shared.Popups;

namespace Content.Server._Respiral.Lock;

public sealed class MedievalKeySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedMedievalLockSystem _medLock = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedievalKeyComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<MedievalKeyComponent, UseKeyDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(EntityUid uid, MedievalKeyComponent component, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target is not { } target)
            return;

        if (!HasComp<MedievalLockComponent>(target))
        {
            var lockEnt = _medLock.GetLock(target);

            if (lockEnt == null || !HasComp<MedievalLockComponent>(lockEnt))
                return;
        }

        if (component.UseSound != null)
            component.Sound = _audio.PlayPvs(component.UseSound, Transform(target).Coordinates)?.Entity;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.UseDuration, new UseKeyDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            DistanceThreshold = 2f
        });

    }

    private void OnDoAfter(EntityUid uid, MedievalKeyComponent component, UseKeyDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target is not { } target)
        {
            component.Sound = _audio.Stop(component.Sound);
            return;
        }

        if (!TryComp<MedievalLockComponent>(target, out var lockComp))
        {
            var lockEnt = _medLock.GetLock(target);

            if (lockEnt == null || !TryComp(lockEnt, out lockComp))
            {
                component.Sound = _audio.Stop(component.Sound);
                return;
            }
            else
            {
                target = lockEnt.Value;
            }
        }

        if (component.IsLockpick)
        {
            if (_medLock.IsLocked(target, lockComp))
            {
                _medLock.UnLock(target, lockComp);
            }
            else
            {
                _medLock.Lock(target, lockComp);
            }

            args.Handled = true;
            component.Sound = _audio.Stop(component.Sound);
            return;
        }

        if (_medLock.IsLocked(target, lockComp))
        {
            if (!_medLock.TryUnlock(target, component.KeyId, lockComp))
                _popup.PopupEntity(Loc.GetString("medieval-door-fail"), args.Args.User, args.Args.User);
        }
        else
        {
            if (!_medLock.TryLock(target, component.KeyId, lockComp))
                _popup.PopupEntity(Loc.GetString("medieval-door-fail"), args.Args.User, args.Args.User);
        }

        component.Sound = _audio.Stop(component.Sound);
        args.Handled = true;
    }

    public void CopyKey(EntityUid present, EntityUid receiver, MedievalKeyComponent? pComp = null, MedievalKeyComponent? rComp = null)
    {
        if (!Resolve(present, ref pComp))
            return;

        if (!Resolve(receiver, ref rComp))
            return;

        rComp.KeyId = pComp.KeyId;
    }

    /// <summary>
    ///     Генерирует уникальный идентификатор ключа для замка или ключа.
    /// </summary>
    public string GenerateKeyId()
    {

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var id = new char[8];
        for (var i = 0; i < id.Length; i++)
        {
            id[i] = chars[_random.Next(chars.Length)];
        }

        return new string(id);
    }
}
