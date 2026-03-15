using Content.Shared.Damage;

namespace Content.Server_Respiral.Wear.Components;

[RegisterComponent]
public sealed partial class LatheWearComponent : Component
{
    /// <summary>
    ///     Стандартное количество добавляемых очков за один произведённый продукт
    /// </summary>
    [DataField]
    public int DefaultAddPoints = 1;

    /// <summary>
    ///     Количество добавляемых очков за еденицу заданного произведённого продукта
    /// </summary>
    [DataField]
    public Dictionary<string, int> AddPoints { get; set; } = new Dictionary<string, int>();

    /// <summary>
    ///     Урон станку при использовании
    /// </summary>
    [DataField]
    public DamageSpecifier Damage = new DamageSpecifier();
}
