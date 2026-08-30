using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._Respiral.Armor;

/// <summary>
/// Средневековая броня: коэффициенты снижения урона ВЫВОДЯТСЯ из материального состава
/// (<c>PhysicalComposition</c> × <c>MaterialPrototype.MedievalProtection</c>). Зона тела не учитывается —
/// защиту задаёт только количество материала и его показатель.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MedievalArmorComponent : Component
{
    /// <summary>
    /// Выведенные коэффициенты снижения урона. Заполняется системой на MapInit, сетевое поле.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageModifierSet Modifiers = new();

    [DataField]
    public bool ShowArmorOnExamine = true;
}
