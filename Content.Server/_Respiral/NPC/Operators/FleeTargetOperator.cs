using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Robust.Shared.Map;

namespace Content._Respiral.NPC.Operators;

/// <summary>
///     Вычисляет координаты на удалении от цели и сохраняет их на доске для перемещения
/// </summary>
public sealed partial class FleeTargetOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;
    private SharedMapSystem _map = default!;
    private SharedTransformSystem _transform = default!;

    [DataField(required: true)]
    public string TargetKey = default!;

    [DataField]
    public string TargetCoordinatesKey = "FleeCoordinates";

    [DataField]
    public float Distance = 8f;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _map = sysManager.GetEntitySystem<SharedMapSystem>();
        _transform = sysManager.GetEntitySystem<SharedTransformSystem>();
    }

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
        if (_map.TryFindGridAt(ownerXform.MapID, fleeWorldPos, out var gridUid, out var mapGrid))
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
