using Content.Server.Parallax;
using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random; // Respiral-ProcGen-Add

namespace Content.Server.Station.Systems;

public sealed partial class StationBiomeSystem : EntitySystem
{
    [Dependency] private BiomeSystem _biome = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private IRobustRandom _random = default!; // Respiral-ProcGen-Add

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationBiomeComponent, StationPostInitEvent>(OnStationPostInit);
    }

    private void OnStationPostInit(Entity<StationBiomeComponent> map, ref StationPostInitEvent args)
    {
        var station = _station.GetLargestGrid(map.Owner);
        if (station == null)
            return;

        var mapId = Transform(station.Value).MapID;
        var mapUid = _map.GetMapOrInvalid(mapId);

        // Respiral-ProcGen-Edit-start
        var minTime = TimeSpan.FromMinutes(15);
        var maxTime = TimeSpan.FromMinutes(30);

        var randomMinutes = minTime.TotalMinutes + _random.NextDouble() * (maxTime.TotalMinutes - minTime.TotalMinutes);
        var offset = TimeSpan.FromMinutes(randomMinutes);

        _biome.EnsurePlanet(mapUid, _proto.Index(map.Comp.Biome), map.Comp.Seed, offset, mapLight: map.Comp.MapLightColor);
        // Respiral-ProcGen-Edit-end
    }
}
