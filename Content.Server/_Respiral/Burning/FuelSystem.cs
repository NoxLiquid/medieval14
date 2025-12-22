using Content.Shared.Burning;

namespace Content.Server.Burning.Systems;

public sealed class FuelSystem : EntitySystem
{
    public FuelType MixFuels(FuelType fuelA, FuelType fuelB, float amountA, float amountB)
    {
        var total = amountA + amountB;
        var ratioA = amountA / total;
        var ratioB = amountB / total;

        return new FuelType(
            fuelA.BaseTemp * ratioA + fuelB.BaseTemp * ratioB,
            fuelA.MaxTemp * ratioA + fuelB.MaxTemp * ratioB,
            fuelA.TempRiseRate * ratioA + fuelB.TempRiseRate * ratioB,
            fuelA.TempDecayRate * ratioA + fuelB.TempDecayRate * ratioB,
            fuelA.BurnRate * ratioA + fuelB.BurnRate * ratioB,
            fuelA.Efficiency * ratioA + fuelB.Efficiency * ratioB
        );
    }
}
