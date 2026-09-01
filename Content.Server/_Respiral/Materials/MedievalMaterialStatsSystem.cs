using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Shared._Respiral.Materials;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Triggers;
using Content.Shared.FixedPoint;
using Content.Shared.Materials;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Prototypes;

namespace Content.Server._Respiral.Materials;

/// <summary>
/// Выводит боевые характеристики предмета из его материального состава (<see cref="PhysicalCompositionComponent"/>)
/// и характеристик материалов (<see cref="MaterialPrototype"/>).
/// </summary>
public sealed partial class MedievalMaterialStatsSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PhysicalCompositionComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<PhysicalCompositionComponent> ent, ref MapInitEvent args)
    {
        ApplyDurability(ent);
        ApplyWeaponDamage(ent);
    }

    /// <summary>
    /// Урон оружия выводится из базового (железный эталон) урона и ОСНОВНОГО материала (с наибольшим
    /// damageFactor — это клинок/боёк, а не деревянная рукоять): итог = base × factor, с опциональной
    /// сменой типа (дерево → Blunt) и бонусом (серебро → +Holy) от основного материала.
    /// </summary>
    private void ApplyWeaponDamage(Entity<PhysicalCompositionComponent> ent)
    {
        if (!TryComp<MedievalWeaponDamageComponent>(ent, out var weapon)
            || !TryComp<MeleeWeaponComponent>(ent, out var melee))
            return;

        MaterialPrototype? primary = null;
        var factor = 0f;
        foreach (var (materialId, _) in ent.Comp.MaterialComposition)
        {
            if (_proto.TryIndex<MaterialPrototype>(materialId, out var material) && material.MedievalDamageFactor > factor)
            {
                primary = material;
                factor = material.MedievalDamageFactor;
            }
        }

        if (primary == null || factor <= 0f)
            return;

        var baseTotal = 0f;
        foreach (var value in weapon.BaseDamage.DamageDict.Values)
            baseTotal += value.Float();

        var result = new DamageSpecifier();

        if (!string.IsNullOrEmpty(primary.MedievalWeaponDamageType))
            result.DamageDict[primary.MedievalWeaponDamageType] = FixedPoint2.New(baseTotal * factor);
        else
            foreach (var (type, value) in weapon.BaseDamage.DamageDict)
                result.DamageDict[type] = value * factor;

        foreach (var (type, fraction) in primary.MedievalWeaponBonusDamage)
        {
            var add = FixedPoint2.New(baseTotal * factor * fraction);
            result.DamageDict[type] = result.DamageDict.TryGetValue(type, out var current) ? current + add : add;
        }

        melee.Damage = result;
        Dirty(ent.Owner, melee);
    }

    /// <summary>
    /// Порог разрушения = Σ(количество материала × его прочность). Заменяет захардкоженный порог,
    /// делая долговечность зависимой от материала и его количества.
    /// </summary>
    private void ApplyDurability(Entity<PhysicalCompositionComponent> ent)
    {
        if (!TryComp<DestructibleComponent>(ent, out var destructible))
            return;

        var durability = 0f;
        foreach (var (materialId, amount) in ent.Comp.MaterialComposition)
        {
            if (_proto.TryIndex<MaterialPrototype>(materialId, out var material))
                durability += amount * material.MedievalDurability;
        }

        if (durability <= 0f)
            return;

        var hp = FixedPoint2.New(durability);
        foreach (var threshold in destructible.Thresholds)
        {
            if (threshold.Trigger is DamageTrigger trigger && IsDestruction(threshold))
                trigger.Damage = hp;
        }
    }

    private static bool IsDestruction(DamageThreshold threshold)
    {
        foreach (var behavior in threshold.Behaviors)
        {
            if (behavior is DoActsBehavior acts && acts.HasAct(ThresholdActs.Destruction))
                return true;
        }

        return false;
    }
}
