using Content.Shared._Respiral.Lock;
using Content.Server._Respiral.Lock.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared._Respiral.Lock.Events;
using Robust.Server.Audio;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Content.Shared.Hands.Components;
using Robust.Shared.Utility;
using Content.Shared.Database;
using Content.Shared._Respiral.Lock.Components;

namespace Content.Server._Respiral.Lock;

public sealed partial class MedievalKeyDuplicatorSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private SharedMedievalLockSystem _medLock = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private MedievalKeySystem _key = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedievalKeyDuplicatorComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<MedievalKeyDuplicatorComponent, DuplicateKeyDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<MedievalKeyDuplicatorComponent, GetVerbsEvent<Verb>>(DoSetVerbs);
    }

    private void DoSetVerbs(EntityUid uid, MedievalKeyDuplicatorComponent component, GetVerbsEvent<Verb> args)
    {
        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (HasComp<HandsComponent>(args.User) && !String.IsNullOrEmpty(component.KeyId))
        {
            args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString("medieval-duplicat-reset"),
                ClientExclusive = true,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
                Act = () => DropDuplicator(uid, component),
                Impact = LogImpact.Medium
            });
        }

        if (component.IsDuplicatorState)
        {
            args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString("medieval-duplicat-mode"),
                ClientExclusive = true,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
                Act = () => { component.IsDuplicatorState = false; },
                Impact = LogImpact.Medium
            });
        }
        else
        {
            args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString("medieval-generate-mode"),
                ClientExclusive = true,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
                Act = () => { component.IsDuplicatorState = true; },
                Impact = LogImpact.Medium
            });
        }
    }

    private void OnAfterInteract(EntityUid uid, MedievalKeyDuplicatorComponent component, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target is not { } target)
            return;

        var owner = target;

        if (_medLock.GetLock(target) is { } lockEnt)
            target = lockEnt;

        if (component.IsDuplicatorState)
        {
            if (!CanDuplicate(uid, target, args.User))
                return;
        }
        else
        {
            if (!CanGenerate(uid, target, args.User))
                return;
        }

        component.Sound = _audio.Stop(component.Sound);

        if (!_doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.UseDuration, new DuplicateKeyDoAfterEvent(), uid, target: owner, used: uid)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            DistanceThreshold = 2f
        }))
            return;

        if (component.UseSound != null)
            component.Sound = _audio.PlayPvs(component.UseSound, Transform(target).Coordinates)?.Entity;
    }

    private void OnDoAfter(EntityUid uid, MedievalKeyDuplicatorComponent component, DuplicateKeyDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target is not { } target)
        {
            component.Sound = _audio.Stop(component.Sound);
            return;
        }

        if (_medLock.GetLock(target) is { } lockEnt)
            target = lockEnt;

        var user = args.Args.User;

        if (component.IsDuplicatorState)
        {
            if (!CanDuplicate(uid, target, user))
            {
                component.Sound = _audio.Stop(component.Sound);
                return;
            }

            if (TryComp<MedievalKeyComponent>(target, out var keyComp))
            {

                if (String.IsNullOrEmpty(component.KeyId) && !String.IsNullOrEmpty(keyComp.KeyId))
                {
                    component.KeyId = keyComp.KeyId;
                    _popup.PopupEntity(Loc.GetString("medieval-duplicat-pass1"), user, user);
                }

                if (!String.IsNullOrEmpty(component.KeyId) && String.IsNullOrEmpty(keyComp.KeyId))
                {
                    keyComp.KeyId = component.KeyId;
                    _popup.PopupEntity(Loc.GetString("medieval-duplicat-pass2"), user, user);
                    return;
                }
            }
            else if (TryComp<MedievalLockComponent>(target, out var lockComp))
            {
                if (!String.IsNullOrEmpty(component.KeyId) && String.IsNullOrEmpty(lockComp.KeyId))
                {
                    _medLock.SetKey(target, component.KeyId, lockComp);
                    _popup.PopupEntity(Loc.GetString("medieval-duplicat-pass3"), user, user);
                    return;
                }
            }
        }
        else
        {
            if (!CanGenerate(uid, target, user))
            {
                component.Sound = _audio.Stop(component.Sound);
                return;
            }

            if (TryComp<MedievalKeyComponent>(target, out var keyComp))
            {
                keyComp.KeyId = _key.GenerateKeyId();
                component.KeyId = keyComp.KeyId;
                _popup.PopupEntity(Loc.GetString("medieval-duplicat-pass2"), user, user);
            }
        }

        component.Sound = _audio.Stop(component.Sound);
        args.Handled = true;
    }

    private bool CanDuplicate(EntityUid uid, EntityUid target, EntityUid user, MedievalKeyDuplicatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (TryComp<MedievalKeyComponent>(target, out var keyComp))
        {
            if (String.IsNullOrEmpty(component.KeyId) && String.IsNullOrEmpty(keyComp.KeyId))
            {
                _popup.PopupEntity(Loc.GetString("medieval-duplicat-fail1"), user, user);
                return false;
            }

            if (!String.IsNullOrEmpty(component.KeyId) && !String.IsNullOrEmpty(keyComp.KeyId))
            {
                _popup.PopupEntity(Loc.GetString("medieval-duplicat-fail2"), user, user);
                return false;
            }
        }
        else if (TryComp<MedievalLockComponent>(target, out var lockComp))
        {
            if (!String.IsNullOrEmpty(lockComp.KeyId))
            {
                _popup.PopupEntity(Loc.GetString("medieval-duplicat-fail3"), user, user);
                return false;
            }

            if (String.IsNullOrEmpty(component.KeyId) && String.IsNullOrEmpty(lockComp.KeyId))
            {
                _popup.PopupEntity(Loc.GetString("medieval-duplicat-fail1"), user, user);
                return false;
            }
        }
        else
        {
            return false;
        }

        return true;
    }

    private bool CanGenerate(EntityUid uid, EntityUid target, EntityUid user, MedievalKeyDuplicatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!TryComp<MedievalKeyComponent>(target, out var keyComp))
            return false;

        if (!String.IsNullOrEmpty(keyComp.KeyId))
        {
            _popup.PopupEntity(Loc.GetString("medieval-duplicat-fail2"), user, user);
            return false;
        }

        return true;
    }

    private void DropDuplicator(EntityUid uid, MedievalKeyDuplicatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.KeyId = string.Empty;
    }
}
