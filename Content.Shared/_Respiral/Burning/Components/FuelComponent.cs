namespace Content.Shared._Respiral.Burning.Components;

[RegisterComponent]
public sealed partial class FuelComponent : Component
{
    [DataField("fuelType")]
    public FuelType FuelType;
}
