using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Shared._Respiral.Materials;
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
    [Dependency] private readonly IPrototypeManager _proto = default!;

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
    /// Урон оружия = базовый (железный эталон) × max(damageFactor) по материалам состава.
    /// Тип урона берётся из BaseDamage, поэтому дерево (Blunt) и серебро (+Holy) сохраняются.
    /// </summary>
    private void ApplyWeaponDamage(Entity<PhysicalCompositionComponent> ent)
    {
        if (!TryComp<MedievalWeaponDamageComponent>(ent, out var weapon)
            || !TryComp<MeleeWeaponComponent>(ent, out var melee))
            return;

        var factor = 0f;
        foreach (var (materialId, _) in ent.Comp.MaterialComposition)
        {
            if (_proto.TryIndex<MaterialPrototype>(materialId, out var material))
                factor = MathF.Max(factor, material.MedievalDamageFactor);
        }

        if (factor <= 0f)
            return;

        melee.Damage = weapon.BaseDamage * factor;
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
