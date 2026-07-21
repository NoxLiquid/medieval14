namespace Content.Server._Respiral.Wear.Components;

/// <summary>
///     Добавьте носителю одежды этот компонент, чтобы он получал урон по одежде при получении определенного количества урона.
///     Урон будет распределяться между всеми предметами одежды, которые носит персонаж и которые находятся в списке слотов.
///     Если у предмета одежды есть компонент <see cref="WearComponent"/>, то он будет получать урон при достижении порога урона, указанного в этом компоненте.
/// </summary>
[RegisterComponent]
public sealed partial class ClothingDamageDistributionComponent : Component
{
    /// <summary>
    ///     Порог урона через который будет наноситься урон по одежде.
    /// </summary>
    [DataField]
    public float DamageThreshold = 10f;

    /// <summary>
    ///     Какие слоты проверять
    /// </summary>
    [DataField]
    public List<string> Slots = new() { "head", "outerClothing", "neck", "gloves", "shoes", "jumpsuit", "underwearb" };
}