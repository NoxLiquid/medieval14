using Content.Server.Cargo.Components;
using Content.Server.Station.Events;
using Content.Server._Respiral.Trade.Components;

namespace Content.Server._Respiral.Trade;

// Respiral - помечает все гриды станции как TradeStation при инициализации
public sealed class MedievalTradeStationSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MedievalTradeStationComponent, StationPostInitEvent>(OnStationPostInit);
    }

    private void OnStationPostInit(Entity<MedievalTradeStationComponent> ent, ref StationPostInitEvent args)
    {
        var data = args.Station.Comp;
        foreach (var gridUid in data.Grids)
        {
            EnsureComp<TradeStationComponent>(gridUid);
        }
    }
}
