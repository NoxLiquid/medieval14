using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._Respiral.Materials;

/// <summary>
/// Базовый (железный эталон) урон оружия. Итоговый <c>MeleeWeapon.Damage</c> = BaseDamage × max(damageFactor)
/// по материалам <c>PhysicalComposition</c>. Тип урона берётся из BaseDamage (металл — Slash, дерево — Blunt,
/// серебро — +Holy), поэтому спец-типы сохраняются. Заполняется на MapInit сервером.
/// </summary>
[RegisterComponent]
public sealed partial class MedievalWeaponDamageComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier BaseDamage = new();
}
