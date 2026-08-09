using Content.Shared._Respiral.Hitch;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;

namespace Content.Server._Respiral.Hitch;

public sealed partial class HitchSystem : EntitySystem
{
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitchComponent, DragDropTargetEvent>(OnDragDropTarget);
        SubscribeLocalEvent<HitchComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<HitchComponent, HitchDoAfterEvent>(OnHitchDoAfter);
        SubscribeLocalEvent<HitchComponent, PullStoppedMessage>(OnPullStopped);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HitchComponent>();
        while (query.MoveNext(out var cart, out var hitch))
        {
            if (hitch.Horse is not { } horse || !Exists(horse))
                continue;

            _transform.SetWorldRotation(cart, _transform.GetWorldRotation(horse));
        }
    }

    private void OnDragDropTarget(Entity<HitchComponent> ent, ref DragDropTargetEvent args)
    {
        if (args.Handled || ent.Comp.Horse != null || !HasComp<HitchableComponent>(args.Dragged))
            return;

        StartAttach(ent, args.Dragged, args.User);
        args.Handled = true;
    }

    private void OnGetVerbs(Entity<HitchComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || ent.Comp.Horse == null)
            return;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("hitch-detach-verb"),
            Act = () => Detach(ent),
            DoContactInteraction = true,
        });
    }

    private void StartAttach(Entity<HitchComponent> ent, EntityUid horse, EntityUid user)
    {
        var doAfter = new DoAfterArgs(EntityManager, user, ent.Comp.AttachDelay,
            new HitchDoAfterEvent(), ent.Owner, horse, ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnHitchDoAfter(Entity<HitchComponent> ent, ref HitchDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } horse)
            return;

        args.Handled = true;

        if (!Exists(horse) || !HasComp<HitchableComponent>(horse))
            return;

        if (TryHitch(ent, horse))
            _popup.PopupEntity(Loc.GetString("hitch-attach-success", ("horse", horse)), ent.Owner, args.User);
        else
            _popup.PopupEntity(Loc.GetString("hitch-attach-fail"), ent.Owner, args.User);
    }

    private bool TryHitch(Entity<HitchComponent> ent, EntityUid horse)
    {
        if (ent.Comp.Horse != null)
            return false;

        var hadPuller = HasComp<PullerComponent>(horse);
        var puller = EnsureComp<PullerComponent>(horse);
        if (!hadPuller)
            puller.NeedsHands = false;

        EnsureComp<PullableComponent>(ent.Owner);

        if (!_pulling.TryStartPull(horse, ent.Owner))
        {
            if (!hadPuller)
                RemComp<PullerComponent>(horse);
            return false;
        }

        ent.Comp.Horse = horse;
        Dirty(ent);
        return true;
    }

    private void Detach(Entity<HitchComponent> ent)
    {
        if (ent.Comp.Horse is not { } horse)
            return;

        ent.Comp.Horse = null;
        Dirty(ent);

        if (TryComp<PullableComponent>(ent.Owner, out var pullable) && pullable.Puller == horse)
            _pulling.TryStopPull(ent.Owner, pullable);
    }

    private void OnPullStopped(EntityUid uid, HitchComponent comp, PullStoppedMessage args)
    {
        if (args.PulledUid == uid)
        {
            comp.Horse = null;
            Dirty(uid, comp);
        }
    }
}
