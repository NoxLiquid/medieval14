using System.Diagnostics.CodeAnalysis;
using Content.Server._Respiral.Precipitation.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Weather;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared._Respiral.Precipitation;
using Content.Shared._Respiral.TimeWindow;
using Content.Shared.Chemistry.Components;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Respiral.Precipitation;

/// <summary>
/// Respiral: осадки на карте (дождь и т.п.). На время осадка включает upstream weather
/// (parallax + звук) и периодически поливает случайные открытые небу тайлы реагентом.
///
/// Интеграция с ZLevels: если карта входит в z-сеть, осадок распространяется на все её уровни —
/// погода ставится на каждую карту, а тайлы для разлива выбираются со всех карт сети.
/// Перекрытие уровней проверять вручную не нужно: CESharedZLevelsRoofSystem уже проставляет
/// нижним тайлам RoofComponent, поэтому CanWeatherAffect сам исключает перекрытые тайлы.
/// </summary>
public sealed partial class PrecipitationSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private PuddleSystem _puddle = default!;
    [Dependency] private WeatherSystem _weather = default!;
    [Dependency] private TimedWindowSystem _timedWindow = default!;
    [Dependency] private CESharedZLevelsSystem _zLevels = default!;

    // Гриды всех покрытых осадком карт (переиспользуется каждый тик разлива)
    private readonly List<Entity<MapGridComponent>> _grids = new();
    // Карты, покрытые текущим осадком: вся z-сеть либо одна карта
    private readonly List<EntityUid> _maps = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MapPrecipitationComponent, MapInitEvent>(OnMapInit);
    }

    // Планируем первый осадок после случайного сухого периода
    private void OnMapInit(Entity<MapPrecipitationComponent> ent, ref MapInitEvent args)
    {
        _timedWindow.Reset(ent.Comp.Interval);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var query = EntityQueryEnumerator<MapPrecipitationComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Сухо — ждём, пока истечёт окно до следующего осадка
            if (comp.Active == null)
            {
                if (_timedWindow.IsExpired(comp.Interval) && comp.Possible.Count > 0)
                    StartPrecipitation((uid, comp), _random.Pick(comp.Possible));

                continue;
            }

            // Осадок закончился — выключаем
            if (now >= comp.EndTime)
            {
                StopPrecipitation((uid, comp));
                continue;
            }

            // Поливаем тайлы порциями
            if (now >= comp.NextSpill)
                SpillTick((uid, comp));
        }
    }

    /// <summary>
    /// Принудительно запускает осадок на карте (напр. из команды). Ставит/обновляет компонент.
    /// Погода включается на всей z-сети карты, если она есть.
    /// </summary>
    public bool StartPrecipitation(Entity<MapPrecipitationComponent?> ent, ProtoId<PrecipitationPrototype> protoId)
    {
        ent.Comp ??= EnsureComp<MapPrecipitationComponent>(ent);

        if (!_proto.TryIndex(protoId, out var proto))
            return false;

        // Длительность осадка = случайное время из окна прототипа
        var duration = _timedWindow.GetRandomTime(proto.Duration) - _timing.CurTime;

        // Включаем weather (parallax + звук) на каждой карте сети
        GetPrecipitationMaps(ent.Owner, _maps);
        foreach (var mapUid in _maps)
        {
            if (TryComp<MapComponent>(mapUid, out var mapComp))
                _weather.TrySetWeather(mapComp.MapId, proto.Weather, out _, duration);
        }

        ent.Comp.Active = protoId;
        ent.Comp.EndTime = _timing.CurTime + duration;
        ent.Comp.NextSpill = _timing.CurTime;
        return true;
    }

    /// <summary>
    /// Останавливает текущий осадок: убирает weather со всех карт сети и планирует следующий сухой период.
    /// </summary>
    public void StopPrecipitation(Entity<MapPrecipitationComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Active != null && _proto.TryIndex(ent.Comp.Active.Value, out var proto))
        {
            GetPrecipitationMaps(ent.Owner, _maps);
            foreach (var mapUid in _maps)
            {
                if (TryComp<MapComponent>(mapUid, out var mapComp))
                    _weather.TryRemoveWeather(mapComp.MapId, proto.Weather);
            }
        }

        ent.Comp.Active = null;
        _timedWindow.Reset(ent.Comp.Interval);
    }

    /// <summary>
    /// Ищет уже существующий <see cref="MapPrecipitationComponent"/> на карте или где-либо в её z-сети.
    /// Нужно, чтобы ручные команды не плодили второй компонент и корректно останавливали осадок,
    /// заведённый на другом уровне.
    /// </summary>
    public bool TryGetNetworkPrecipitation(EntityUid mapUid, out Entity<MapPrecipitationComponent> ent)
    {
        GetPrecipitationMaps(mapUid, _maps);
        foreach (var candidate in _maps)
        {
            if (TryComp<MapPrecipitationComponent>(candidate, out var comp))
            {
                ent = (candidate, comp);
                return true;
            }
        }

        ent = default;
        return false;
    }

    private void SpillTick(Entity<MapPrecipitationComponent> ent)
    {
        if (ent.Comp.Active == null || !_proto.TryIndex(ent.Comp.Active.Value, out var proto))
            return;

        CollectGrids(ent.Owner);

        if (_grids.Count > 0)
        {
            for (var i = 0; i < proto.TilesPerTick; i++)
            {
                if (!TryPickExposedTile(out var tileRef))
                    continue;

                // Каждому тайлу — своя порция реагента
                var solution = new Solution(proto.Reagent, proto.PuddleAmount);
                _puddle.TrySpillAt(tileRef.Value, solution, out _, sound: false);
            }
        }

        ent.Comp.NextSpill = _timing.CurTime + proto.SpillInterval;
    }

    // Карты, на которые действует осадок: вся z-сеть, если карта в ней, иначе только она сама
    private void GetPrecipitationMaps(EntityUid mapUid, List<EntityUid> result)
    {
        result.Clear();
        result.Add(mapUid);

        if (TryComp<CEZMapComponent>(mapUid, out var zMap))
        {
            result.AddRange(_zLevels.GetAllMapsAbove((mapUid, zMap)));
            result.AddRange(_zLevels.GetAllMapsBelow((mapUid, zMap)));
        }
    }

    // Собирает гриды всех покрытых осадком карт в _grids
    private void CollectGrids(EntityUid mapUid)
    {
        GetPrecipitationMaps(mapUid, _maps);

        _grids.Clear();
        var gridQuery = EntityQueryEnumerator<MapGridComponent, TransformComponent>();
        while (gridQuery.MoveNext(out var gridUid, out var grid, out var xform))
        {
            if (xform.MapUid is { } gridMap && _maps.Contains(gridMap))
                _grids.Add((gridUid, grid));
        }
    }

    // Случайный тайл на одном из собранных гридов: не пустой, открыт небу и не впитывающий
    private bool TryPickExposedTile([NotNullWhen(true)] out TileRef? tile)
    {
        tile = null;

        if (_grids.Count == 0)
            return false;

        var (chosenUid, chosenGrid) = _random.Pick(_grids);
        var aabb = chosenGrid.LocalAABB;

        // Несколько попыток попасть в подходящий тайл в пределах грида
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var x = _random.Next((int)MathF.Floor(aabb.Left), (int)MathF.Ceiling(aabb.Right));
            var y = _random.Next((int)MathF.Floor(aabb.Bottom), (int)MathF.Ceiling(aabb.Top));
            var tileRef = _map.GetTileRef(chosenUid, chosenGrid, new Vector2i(x, y));

            if (tileRef.Tile.IsEmpty)
                continue;

            // Крыша (в т.ч. проставленная ZLevels нижним тайлам под перекрытием), Weather-флаг и BlockWeather —
            // всё учтено в CanWeatherAffect, поэтому перекрытые верхними уровнями тайлы сюда не попадут
            if (!_weather.CanWeatherAffect((chosenUid, chosenGrid, null), tileRef))
                continue;

            // Впитывающие тайлы дождь не мочит (по ТЗ)
            if (_turf.GetContentTileDefinition(tileRef).Absorbent)
                continue;

            tile = tileRef;
            return true;
        }

        return false;
    }
}
