namespace Content.Shared.Burning.Components;

[RegisterComponent]
public sealed partial class DamageableBurnerComponent : Component
{
    [DataField("condition")]
    public float Condition = 1.0f;
}
