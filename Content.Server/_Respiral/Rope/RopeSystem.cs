using Content.Shared._Respiral.Rope;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Prototypes;

namespace Content.Server._Respiral.Rope;

public sealed partial class RopeSystem : EntitySystem
{
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    private static readonly ProtoId<TagPrototype> BindableTag = "MedievalRopeBindable";
    private const string RopeContainerId = "rope_knot";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RopeComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<RopeComponent, TieRopeDoAfterEvent>(OnTieDoAfter);
        SubscribeLocalEvent<RopedComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnAfterInteract(Entity<RopeComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (target == args.User || !HasComp<PhysicsComponent>(target))
            return;

        if (ent.Comp.PendingTarget is not { } first || !Exists(first) || first == target)
        {
            ent.Comp.PendingTarget = target;
            _popup.PopupEntity(Loc.GetString("rope-loop-around", ("target", target)), target, args.User);
            args.Handled = true;
            return;
        }

        ent.Comp.PendingTarget = null;
        args.Handled = true;

        if (!_tag.HasTag(first, BindableTag) && !_tag.HasTag(target, BindableTag))
        {
            _popup.PopupEntity(Loc.GetString("rope-not-bindable", ("target", target)), target, args.User);
            return;
        }

        if (HasComp<RopedComponent>(first) || HasComp<RopedComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("rope-tie-fail"), target, args.User);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.TieDelay,
            new TieRopeDoAfterEvent(GetNetEntity(first)), ent.Owner, target, ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnTieDoAfter(Entity<RopeComponent> ent, ref TieRopeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        args.Handled = true;

        var first = GetEntity(args.First);
        if (!Exists(first) || !Exists(target) || first == target)
            return;

        if (TryTie(first, target, ent))
            _popup.PopupEntity(Loc.GetString("rope-tie-success", ("a", first), ("b", target)), target, args.User);
        else
            _popup.PopupEntity(Loc.GetString("rope-tie-fail"), target, args.User);
    }

    private bool TryTie(EntityUid a, EntityUid b, Entity<RopeComponent> rope)
    {
        if (HasComp<RopedComponent>(a) || HasComp<RopedComponent>(b))
            return false;

        var hadPuller = HasComp<PullerComponent>(a);
        var puller = EnsureComp<PullerComponent>(a);
        if (!hadPuller)
            puller.NeedsHands = false;

        var pullable = EnsureComp<PullableComponent>(b);

        if (!_pulling.TryStartPull(a, b))
        {
            if (!hadPuller)
                RemComp<PullerComponent>(a);
            return false;
        }

        ShortenTether(b, pullable, rope.Comp.TetherDistanceDivisor);

        var container = _container.EnsureContainer<ContainerSlot>(a, RopeContainerId);
        if (!_container.Insert(rope.Owner, container))
        {
            StopPull(a, b);
            if (!hadPuller)
                RemComp<PullerComponent>(a);
            return false;
        }

        var ra = EnsureComp<RopedComponent>(a);
        ra.Partner = b;
        ra.Rope = rope.Owner;
        Dirty(a, ra);

        var rb = EnsureComp<RopedComponent>(b);
        rb.Partner = a;
        rb.Rope = rope.Owner;
        Dirty(b, rb);

        var visuals = EnsureComp<JointVisualsComponent>(a);
        visuals.Sprite = rope.Comp.RopeVisual;
        visuals.Target = b;
        Dirty(a, visuals);

        return true;
    }

    private void OnGetVerbs(Entity<RopedComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var target = ent.Owner;
        var user = args.User;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("rope-untie-verb"),
            Act = () => Untie(target, user),
            DoContactInteraction = true,
        });
    }

    private void Untie(EntityUid entity, EntityUid user)
    {
        if (!TryComp<RopedComponent>(entity, out var roped))
            return;

        var partner = roped.Partner;
        var rope = roped.Rope;

        RemComp<RopedComponent>(entity);
        RemComp<JointVisualsComponent>(entity);

        if (partner is { } p && Exists(p))
        {
            RemComp<RopedComponent>(p);
            RemComp<JointVisualsComponent>(p);
        }

        StopPull(entity, partner);

        if (rope is { } r && Exists(r))
        {
            _container.TryRemoveFromContainer(r);
            _hands.TryPickupAnyHand(user, r);
        }
    }

    private void ShortenTether(EntityUid pullable, PullableComponent pullableComp, float divisor)
    {
        if (divisor <= 0f || pullableComp.PullJointId is not { } jointId)
            return;

        if (!TryComp<JointComponent>(pullable, out var jointComp))
            return;

        if (!jointComp.GetJoints.TryGetValue(jointId, out var joint))
            return;

        if (joint is not DistanceJoint distance)
            return;

        distance.Length /= divisor;
        distance.MaxLength /= divisor;
        Dirty(pullable, jointComp);
    }

    private void StopPull(EntityUid a, EntityUid? b)
    {
        if (b is not { } partner)
            return;

        if (TryComp<PullableComponent>(partner, out var partnerPull) && partnerPull.Puller == a)
            _pulling.TryStopPull(partner, partnerPull);

        if (TryComp<PullableComponent>(a, out var aPull) && aPull.Puller == partner)
            _pulling.TryStopPull(a, aPull);
    }
}
