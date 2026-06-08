using System.Numerics;
using Content._Respiral.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared._Respiral.TimeWindow;
using Content.Shared.Damage.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content._Respiral.NPC.Systems;

public sealed class HerbivoreFearSystem : EntitySystem
{
    [Dependency] private HTNSystem _htn = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IMapManager _mapManager = default!;
    [Dependency] private Robust.Shared.Random.IRobustRandom _random = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TimedWindowSystem _timedWindow = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HerbivoreFearComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(Entity<HerbivoreFearComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || !TryComp<HTNComponent>(ent, out var htn))
            return;

        var blackboard = htn.Blackboard;
        blackboard.SetValue(MedievalNPCBlackboard.FearEndTime, _timing.CurTime + _timedWindow.GetRandomTime(ent.Comp.WindowDuration));
        blackboard.SetValue(MedievalNPCBlackboard.FearCoordinates, GetFleeCoordinates(ent.Owner, args.Origin, ent.Comp.FleeDistance));
        _htn.Replan(htn);
    }

    private EntityCoordinates GetFleeCoordinates(EntityUid owner, EntityUid? origin, float distance)
    {
        if (!TryComp(owner, out TransformComponent? ownerXform) || ownerXform.MapUid == null)
            return EntityCoordinates.Invalid;

        var ownerPos = _transform.GetWorldPosition(ownerXform);
        Vector2 fleeDirection;

        if (origin != null && TryComp(origin.Value, out TransformComponent? originXform) && originXform.MapUid == ownerXform.MapUid)
        {
            fleeDirection = ownerPos - _transform.GetWorldPosition(originXform);
        }
        else
        {
            fleeDirection = _random.NextAngle().ToVec();
        }

        if (fleeDirection == Vector2.Zero)
            fleeDirection = Vector2.UnitX;

        fleeDirection = Vector2.Normalize(fleeDirection);
        var fleeWorldPos = ownerPos + fleeDirection * Math.Max(distance, 0.5f);

        if (_mapManager.TryFindGridAt(ownerXform.MapID, fleeWorldPos, out var gridUid, out var mapGrid))
            return new EntityCoordinates(gridUid, _map.WorldToLocal(gridUid, mapGrid, fleeWorldPos));

        return new EntityCoordinates(ownerXform.MapUid.Value, fleeWorldPos);
    }
}