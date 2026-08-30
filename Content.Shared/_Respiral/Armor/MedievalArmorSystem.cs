using System.Linq;
using Content.Shared.Armor;
using Content.Shared.Clothing.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Materials;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Respiral.Armor;

/// <summary>
/// Выводит коэффициенты брони средневековой одежды из материального состава и региона и применяет их
/// к урону носителя (аналог upstream <see cref="SharedArmorSystem"/>
/// </summary>
public sealed partial class MedievalArmorSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ExamineSystemShared _examine = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedievalArmorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MedievalArmorComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnDamageModify);
        SubscribeLocalEvent<MedievalArmorComponent, InventoryRelayedEvent<CoefficientQueryEvent>>(OnCoefficientQuery);
        SubscribeLocalEvent<MedievalArmorComponent, GetVerbsEvent<ExamineVerb>>(OnArmorVerbExamine);
    }

    private void OnMapInit(Entity<MedievalArmorComponent> ent, ref MapInitEvent args)
    {
        Derive(ent);
    }

    /// <summary>
    /// coefₜ = clamp(1 − Σ(amountᵢ/100 × matᵢ.protection[t]), 0.4, 1). Зона тела не учитывается.
    /// </summary>
    private void Derive(Entity<MedievalArmorComponent> ent)
    {
        if (!TryComp<PhysicalCompositionComponent>(ent, out var composition))
            return;

        var protection = new Dictionary<string, float>();

        foreach (var (materialId, amount) in composition.MaterialComposition)
        {
            if (!_proto.TryIndex<MaterialPrototype>(materialId, out var material))
                continue;

            foreach (var (type, value) in material.MedievalProtection)
                protection[type] = protection.GetValueOrDefault(type) + amount / 100f * value;
        }

        var modifiers = new DamageModifierSet();
        foreach (var (type, value) in protection)
            modifiers.Coefficients[type] = Math.Clamp(1f - value, 0.4f, 1f);

        ent.Comp.Modifiers = modifiers;
        Dirty(ent);
    }

    private void OnDamageModify(Entity<MedievalArmorComponent> ent, ref InventoryRelayedEvent<DamageModifyEvent> args)
    {
        if (TryComp<MaskComponent>(ent, out var mask) && mask.IsToggled)
            return;

        args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, ent.Comp.Modifiers);
    }

    private void OnCoefficientQuery(Entity<MedievalArmorComponent> ent, ref InventoryRelayedEvent<CoefficientQueryEvent> args)
    {
        if (TryComp<MaskComponent>(ent, out var mask) && mask.IsToggled)
            return;

        foreach (var (type, value) in ent.Comp.Modifiers.Coefficients)
        {
            args.Args.DamageModifiers.Coefficients[type] =
                args.Args.DamageModifiers.Coefficients.TryGetValue(type, out var existing) ? existing * value : value;
        }
    }

    private void OnArmorVerbExamine(Entity<MedievalArmorComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || !ent.Comp.ShowArmorOnExamine)
            return;

        if (!ent.Comp.Modifiers.Coefficients.Any())
            return;

        var markup = GetArmorExamine(ent.Comp.Modifiers);
        _examine.AddDetailedExamineVerb(args, ent.Comp, markup,
            Loc.GetString("armor-examinable-verb-text"), "/Textures/Interface/VerbIcons/dot.svg.192dpi.png",
            Loc.GetString("armor-examinable-verb-message"));
    }

    private FormattedMessage GetArmorExamine(DamageModifierSet modifiers)
    {
        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(Loc.GetString("armor-examine"));

        foreach (var coefficient in modifiers.Coefficients)
        {
            msg.PushNewline();
            var type = Loc.GetString("armor-damage-type-" + coefficient.Key.ToLower());
            msg.AddMarkupOrThrow(Loc.GetString("armor-coefficient-value",
                ("type", type),
                ("value", MathF.Round((1f - coefficient.Value) * 100, 1))));
        }

        return msg;
    }
}
