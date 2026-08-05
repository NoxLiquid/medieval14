using Content.Shared.Light.Components;
using Robust.Client.Placement;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;

namespace Content.Client._Respiral.Placement;

/// <summary>
///     Хиджек удаления для панели спавна сущностей («крыши»): вместо обычного удаления сущности
///     под курсором стирает ТОЛЬКО сущности с <see cref="IsRoofComponent"/> на тайле клика,
///     не трогая пол/стены/мебель под крышей. Крыши легко заспавнить (SnapgridCenter +
///     PlacementReplacement), но обычный ластик обычно попадает не в крышу, а в то, что под ней —
///     этот режим решает именно это.
/// </summary>
public sealed class MedievalRoofEraserHijack : PlacementHijack
{
    private readonly IEntityManager _entManager;
    private readonly IClientNetManager _netManager;

    public MedievalRoofEraserHijack(IEntityManager entManager, IClientNetManager netManager)
    {
        _entManager = entManager;
        _netManager = netManager;
    }

    public override bool HijackDeletion(EntityCoordinates coordinates)
    {
        // Всегда "поглощаем" клик - не даём обычному по-сущностному удалению сработать по тому,
        // что клиент случайно определил как сущность под курсором (не крышу).
        if (coordinates.GetGridUid(_entManager) is not { } gridUid ||
            !_entManager.TryGetComponent<MapGridComponent>(gridUid, out var gridComp))
        {
            return true;
        }

        var mapSystem = _entManager.System<SharedMapSystem>();

        foreach (var uid in mapSystem.GetAnchoredEntities((gridUid, gridComp), coordinates))
        {
            if (!_entManager.HasComponent<IsRoofComponent>(uid))
                continue;

            var msg = new MsgPlacement
            {
                PlaceType = PlacementManagerMessage.RequestEntRemove,
                EntityUid = _entManager.GetNetEntity(uid),
            };
            _netManager.ClientSendMessage(msg);
        }

        return true;
    }
}
