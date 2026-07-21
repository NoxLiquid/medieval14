namespace Content.Shared._Respiral.Burning.Components;

[RegisterComponent]
public sealed partial class FuelBurnerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Ignited = false;

    [DataField]
    public FuelType? FuelType;

    [ViewVariables(VVAccess.ReadWrite)]
    public float FuelAmount = 0.0f;

    [DataField]
    public float FuelCapacity = 30f;

    [DataField]
    public float DamageRate = 0.01f;

    [DataField]
    public float TempRiseRate = 10.0f;

    [DataField]
    public float TempDecayRate = 1.2f;

    [DataField]
    public float BurnRate = 0.015f;

    [DataField]
    public float FuelEfficiency = 1.0f;

    [DataField]
    public float MaxTemperature = 900f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float CurrentTemperature = 0.0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Ventilation = 0.0f;

    [DataField]
    public bool PassiveVentilation;

    [DataField]
    public float PassiveVentilationRate = 0.5f;

    public TimeSpan LastDamageTime;
}
