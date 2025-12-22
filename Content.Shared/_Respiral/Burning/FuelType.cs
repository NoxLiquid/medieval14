namespace Content.Shared.Burning;

[DataRecord]
public record struct FuelType(
    float BaseTemp,
    float MaxTemp,
    float TempRiseRate,
    float TempDecayRate,
    float BurnRate,
    float Efficiency
);
