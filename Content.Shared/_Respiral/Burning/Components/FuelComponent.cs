namespace Content.Shared.Burning.Components;

[RegisterComponent]
public sealed partial class FuelComponent : Component
{
    [DataField("fuelType")]
    public FuelType FuelType;
}
