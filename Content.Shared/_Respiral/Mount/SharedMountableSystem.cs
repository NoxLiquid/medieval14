using Content.Shared._Respiral.Mount.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Robust.Shared.Physics.Events;

namespace Content.Shared._Respiral.Mount;

public abstract partial class SharedMountSystem : EntitySystem
{
    public const string SaddleSlot = "saddle";

    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private SharedMoverController _mover = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RiderComponent, MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<RiderComponent, UpdateCanMoveEvent>(OnRiderCanMove, after: new[] { typeof(SharedBuckleSystem) });
        SubscribeLocalEvent<RiderComponent, PreventCollideEvent>(OnRiderPreventCollide);

        SubscribeLocalEvent<MountableComponent, StrapAttemptEvent>(OnStrapAttempt);
        SubscribeLocalEvent<MountableComponent, DownAttemptEvent>(OnDownAttempt);
        SubscribeLocalEvent<MountableComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<MountableComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<MountableComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnStrapAttempt(Entity<MountableComponent> ent, ref StrapAttemptEvent args)
    {
        var rider = args.Buckle.Owner;

        // Refuse to mount a saddle-required mount that has no saddle equipped.
        if (ent.Comp.NeedsSaddle && !_inventory.TryGetSlotEntity(ent.Owner, SaddleSlot, out _))
        {
            args.Cancelled = true;
            if (args.Popup && args.User is { } saddleUser)
                _popup.PopupEntity(Loc.GetString("mount-needs-saddle", ("mount", ent.Owner)), ent.Owner, saddleUser);
            return;
        }

        if (IsPullLinked(rider, ent.Owner))
        {
            args.Cancelled = true;
            if (args.Popup && args.User is { } pullUser)
                _popup.PopupEntity(Loc.GetString("mount-stop-pulling", ("mount", ent.Owner)), ent.Owner, pullUser);
        }
    }

    private bool IsPullLinked(EntityUid rider, EntityUid mount)
    {
        if (TryComp<PullableComponent>(mount, out var mountPullable) && mountPullable.Puller == rider)
            return true;

        if (TryComp<PullableComponent>(rider, out var riderPullable) && riderPullable.Puller == mount)
            return true;

        return false;
    }

    private void OnRiderCanMove(EntityUid uid, RiderComponent component, UpdateCanMoveEvent args)
    {
        args.Uncancel();
    }

    private void OnRiderPreventCollide(Entity<RiderComponent> ent, ref PreventCollideEvent args)
    {
        if (ent.Comp.Mount == args.OtherEntity)
            args.Cancelled = true;
    }

    private void OnDownAttempt(Entity<MountableComponent> ent, ref DownAttemptEvent args)
    {
        if (ent.Comp.Rider != null)
            args.Cancel();
    }

    private void OnStrapped(Entity<MountableComponent> ent, ref StrappedEvent args)
    {
        if (_mobState.IsDead(ent))
        {
            _buckle.TryUnbuckle(args.Buckle.Owner, ent.Owner);
            return;
        }

        EnsureComp<InputMoverComponent>(ent);
        var rider = EnsureComp<RiderComponent>(args.Buckle.Owner);
        rider.Mount = ent.Owner;
        ent.Comp.Rider = args.Buckle.Owner;

        Dirty(ent.Owner, ent.Comp);
        Dirty(args.Buckle.Owner, rider);

        _actionBlocker.UpdateCanMove(args.Buckle.Owner);

        if (ent.Comp.ControlMovement)
        {
            _mover.SetRelay(args.Buckle.Owner, ent.Owner);
        }

        if (_standing.IsDown(ent.Owner))
            _standing.Stand(ent.Owner);

        OnRiderMounted(ent.Owner, args.Buckle.Owner);

        if (!TryComp<MovementSpeedModifierComponent>(ent, out var move))
            return;

        var walk = move.BaseWalkSpeed * ent.Comp.MountedSpeed;
        var sprint = move.BaseSprintSpeed * ent.Comp.MountedSpeed;
        _movement.ChangeBaseSpeed(ent, walk, sprint, move.Acceleration, move);
    }

    protected virtual void OnRiderMounted(EntityUid mount, EntityUid rider)
    {
    }

    protected virtual void OnRiderDismounted(EntityUid mount, EntityUid rider)
    {
    }

    private void OnUnstrapped(Entity<MountableComponent> ent, ref UnstrappedEvent args)
    {
        if (HasComp<RelayInputMoverComponent>(args.Buckle.Owner))
            RemComp<RelayInputMoverComponent>(args.Buckle.Owner);

        RemComp<RiderComponent>(args.Buckle.Owner);
        ent.Comp.Rider = null;

        Dirty(ent.Owner, ent.Comp);

        _actionBlocker.UpdateCanMove(args.Buckle.Owner);

        OnRiderDismounted(ent.Owner, args.Buckle.Owner);

        if (!TryComp<MovementSpeedModifierComponent>(ent, out var move))
            return;

        var walk = move.BaseWalkSpeed / ent.Comp.DefaultSpeed;
        var sprint = move.BaseSprintSpeed / ent.Comp.DefaultSpeed;
        _movement.ChangeBaseSpeed(ent, walk, sprint, move.Acceleration, move);
    }

    private void OnMobStateChanged(Entity<RiderComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive || ent.Comp.Mount == null)
            return;

        _buckle.TryUnbuckle(ent.Owner, ent.Comp.Mount);
    }

    private void OnMobStateChanged(Entity<MountableComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive)
        {
            ent.Comp.ControlMovement = false;
            if (ent.Comp.Rider != null)
                _buckle.TryUnbuckle(ent.Comp.Rider.Value, ent.Owner);
        }
        else
        {
            ent.Comp.ControlMovement = true;
        }
    }
}
