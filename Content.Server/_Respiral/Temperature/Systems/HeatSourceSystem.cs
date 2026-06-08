using Content.Server.Temperature.Components;
using Content.Shared.Placeable;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;

namespace Content.Server.Temperature.Systems;

public sealed class HeatSourceSystem : SharedHeatSourceSystem
{
    [Dependency] private TemperatureSystem _temperature = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float deltaTime)
    {
        var query = EntityQueryEnumerator<HeatSourceComponent, ItemPlacerComponent>();
        while (query.MoveNext(out var uid, out var heatComp, out var placer))
        {
            if (!heatComp.IsActive)
                continue;

            var deltaTemp = heatComp.DegreesPerSecond * deltaTime;
            foreach (var ent in placer.PlacedEntities)
            {
                TemperatureComponent? tempComp = null;
                if (!Resolve(ent, ref tempComp))
                    return;

                float newTemp = tempComp.CurrentTemperature + deltaTemp;
                _temperature.ForceChangeTemperature(ent, newTemp, tempComp);
            }
        }
    }
}