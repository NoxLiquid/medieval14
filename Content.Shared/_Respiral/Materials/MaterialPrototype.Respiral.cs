namespace Content.Shared.Materials;

// Respiral — расширение прототипа материала боевыми характеристиками. Из них выводятся
// прочность (Destructible), защита (Armor) и урон оружия по составу (PhysicalComposition).
public sealed partial class MaterialPrototype
{
    /// <summary>
    /// Прочность на единицу материала. Порог разрушения предмета = Σ(количество × прочность).
    /// </summary>
    [DataField]
    public float MedievalDurability = 1f;

    /// <summary>
    /// Защита по типам урона (доля снижения при 100 ед. материала в зоне Body). Из неё выводятся
    /// коэффициенты <c>MedievalArmor</c> с учётом региона и количества материала.
    /// </summary>
    [DataField]
    public Dictionary<string, float> MedievalProtection = new();

    /// <summary>
    /// Множитель урона оружия. Итоговый урон = базовый (железный эталон) × damageFactor основного материала.
    /// </summary>
    [DataField]
    public float MedievalDamageFactor = 1f;

    /// <summary>
    /// Если задан — оружие из этого материала наносит урон ТОЛЬКО этого типа (базовый урон сворачивается
    /// в один тип). Напр. дерево → Blunt.
    /// </summary>
    [DataField]
    public string? MedievalWeaponDamageType;

    /// <summary>
    /// Бонусные типы урона от материала как доля от суммарного базового урона (× damageFactor).
    /// Напр. серебро → { Holy: 1.0 } (добавляет Holy, равный базовому урону).
    /// </summary>
    [DataField]
    public Dictionary<string, float> MedievalWeaponBonusDamage = new();
}
