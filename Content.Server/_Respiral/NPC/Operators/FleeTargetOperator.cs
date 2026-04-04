using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Robust.Shared.Map;

namespace Content._Respiral.NPC.Operators;

/// <summary>
/// Computes coordinates away from a target and stores them on the blackboard for movement.
/// </summary>
public sealed partial class FleeTargetOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    [DataField("targetKey", required: true)]
    public string TargetKey = default!;

    [DataField("targetCoordinatesKey")]
    public string TargetCoordinatesKey = "FleeCoordinates";

    [DataField]
    public float Distance = 8f;

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard, CancellationToken cancelToken)
    {
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, _entManager) ||
            !blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager) ||
            !_entManager.TryGetComponent<TransformComponent>(owner, out var ownerXform) ||
            !_entManager.TryGetComponent<TransformComponent>(target, out var targetXform) ||
            ownerXform.MapUid == null)
        {
            return (false, null);
        }

        var ownerPos = _transform.GetWorldPosition(ownerXform);
        var targetPos = _transform.GetWorldPosition(targetXform);
        var fleeDirection = ownerPos - targetPos;

        if (fleeDirection == Vector2.Zero)
            fleeDirection = Vector2.UnitX;

        fleeDirection = Vector2.Normalize(fleeDirection);
        var fleeWorldPos = ownerPos + fleeDirection * Math.Max(Distance, 0.5f);

        EntityCoordinates fleeCoordinates;
        if (_mapManager.TryFindGridAt(ownerXform.MapID, fleeWorldPos, out var gridUid, out var mapGrid))
        {
            fleeCoordinates = new EntityCoordinates(gridUid, _map.WorldToLocal(gridUid, mapGrid, fleeWorldPos));
        }
        else
        {
            fleeCoordinates = new EntityCoordinates(ownerXform.MapUid.Value, fleeWorldPos);
        }

        return (true, new Dictionary<string, object>
        {
            {TargetCoordinatesKey, fleeCoordinates},
        });
    }
}