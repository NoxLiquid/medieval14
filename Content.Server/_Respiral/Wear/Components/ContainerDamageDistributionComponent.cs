namespace Content.Server._Respiral.Wear.Components;

[RegisterComponent]
public sealed partial class ContainerDamageDistributionComponent : Component
{
    /// <summary>
    ///     Идентификатор контейнера, к которому будет применяться распределение урона.
    /// </summary>
    [DataField(required: true)]
    public string ContainerId = string.Empty;

    /// <summary>
    ///     Порог урона через который будет наноситься урон по предметам в контейнере.
    /// </summary>
    [DataField]
    public float DamageThreshold = 10f;
}