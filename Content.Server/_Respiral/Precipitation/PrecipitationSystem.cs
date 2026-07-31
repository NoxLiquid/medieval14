using Content.Server._Respiral.Precipitation.Components;
using Content.Server.Weather;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared._Respiral.Precipitation;
using Content.Shared._Respiral.TimeWindow;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Respiral.Precipitation;

/// <summary>
/// Respiral: осадки на карте (дождь и т.п.). На время осадка включает upstream weather
/// (parallax + звук).
///
/// Интеграция с ZLevels: если карта входит в z-сеть, осадок распространяется на все её уровни —
/// погода ставится на каждую карту сети.
/// </summary>
public sealed partial class PrecipitationSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private WeatherSystem _weather = default!;
    [Dependency] private TimedWindowSystem _timedWindow = default!;
    [Dependency] private CESharedZLevelsSystem _zLevels = default!;

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
                StopPrecipitation((uid, comp));
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
}
