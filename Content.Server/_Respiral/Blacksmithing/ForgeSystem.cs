using System.Linq;
using Content.Server.Burning.Systems;
using Content.Server.Damage.Systems;
using Content.Shared._Respiral.Blacksmithing;
using Content.Shared._Respiral.Blacksmithing.Components;
using Content.Shared.Burning.Components;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server._Respiral.Blacksmithing.Systems;

public sealed partial class ForgeSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private FuelBurnerSystem _burner = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] private SharedPointLightSystem _light = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private QuenchingSystem _quenching = default!;
    [Dependency] private StaminaSystem _stamina = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TongsComponent, MapInitEvent>(OnTongsMapInit);
        SubscribeLocalEvent<TongsComponent, GotUnequippedHandEvent>(OnTongsUnequipped);
        SubscribeLocalEvent<TongsComponent, AfterInteractEvent>(OnTongsInteract);

        SubscribeLocalEvent<BellowsComponent, AfterInteractEvent>(OnBellowsInteract);

        SubscribeLocalEvent<ForgeComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<ForgeComponent, ItemRemovedEvent>(OnItemRemoved);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var forgeQuery = EntityQueryEnumerator<ForgeComponent, FuelBurnerComponent, ItemPlacerComponent>();
        while (forgeQuery.MoveNext(out var uid, out _, out var burner, out var placer))
        {
            _burner.UpdateBurner(uid, burner, frameTime);

            if (burner.CurrentTemperature > 0 && placer.PlacedEntities.Count > 0)
                HeatIngotsOnForge(burner, placer, frameTime);
        }

        var heatedQuery = EntityQueryEnumerator<HeatedComponent, HeatedIngotComponent>();
        while (heatedQuery.MoveNext(out var uid, out var heated, out _))
        {
            if (!IsOnHeater(uid))
            {
                _burner.CoolObject(uid, heated, frameTime);
                UpdateIngotAppearance(uid, heated);
            }
        }
    }

    #region Tongs

    private void OnTongsMapInit(EntityUid uid, TongsComponent comp, MapInitEvent args)
    {
        comp.TongsContainer = _container.EnsureContainer<Container>(uid, TongsComponent.TongsContainerName);
    }

    private void OnTongsUnequipped(EntityUid uid, TongsComponent comp, GotUnequippedHandEvent args)
    {
        if (comp.HeldIngot == null)
            return;

        _container.EmptyContainer(comp.TongsContainer);
        comp.HeldIngot = null;
    }

    private void OnTongsInteract(EntityUid uid, TongsComponent comp, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (comp.HeldIngot != null && HasComp<QuenchingComponent>(args.Target.Value)
            && _quenching.TryQuench(args.User, comp.HeldIngot.Value, args.Target.Value, out var result))
        {
            _container.EmptyContainer(comp.TongsContainer);
            _transform.SetCoordinates(result.Value, Transform(args.Target.Value).Coordinates);
            comp.HeldIngot = null;

            args.Handled = true;
        }
        else if (comp.HeldIngot != null)
        {
            PlaceIngot(comp, args.Target.Value, args.User);
            args.Handled = true;
        }
        else if (HasComp<HeatedIngotComponent>(args.Target.Value))
        {
            PickupIngot(comp, args.Target.Value, args.User);
            args.Handled = true;
        }
    }

    private void PickupIngot(TongsComponent comp, EntityUid ingot, EntityUid user)
    {
        comp.HeldIngot = ingot;
        _container.Insert(ingot, comp.TongsContainer);

        _popup.PopupEntity(Loc.GetString("tongs-ingot-inserted", ("ingot", Name(ingot))), user, user);
    }

    private void PlaceIngot(TongsComponent comp, EntityUid target, EntityUid user)
    {
        if (comp.HeldIngot == null)
            return;

        _container.EmptyContainer(comp.TongsContainer);
        _transform.SetCoordinates(comp.HeldIngot.Value, Transform(target).Coordinates);

        _popup.PopupEntity(Loc.GetString("tongs-ingot-placed", ("ingot", Name(comp.HeldIngot.Value))), user, user);

        comp.HeldIngot = null;
    }

    #endregion

    #region Bellows

    private void OnBellowsInteract(EntityUid uid, BellowsComponent comp, AfterInteractEvent args)
    {
        if (!TryComp<FuelBurnerComponent>(args.Target, out var burner) || burner.PassiveVentilation)
            return;

        UseBellowsOnBurner(args.User, args.Target.Value, uid, comp.AirFlow, burner);
        args.Handled = true;
    }

    public void UseBellowsOnBurner(EntityUid user, EntityUid burner, EntityUid bellows, float airFlow, FuelBurnerComponent? burnerComp = null)
    {
        if (!Resolve(burner, ref burnerComp))
            return;

        if (!TryComp<UseDelayComponent>(bellows, out var useDelay) || _useDelay.IsDelayed((bellows, useDelay)))
            return;

        burnerComp.Ventilation += airFlow;

        _stamina.TryTakeStamina(user, 10f);
        _useDelay.TryResetDelay((bellows, useDelay));
    }

    #endregion

    #region Forge

    private void OnItemPlaced(EntityUid uid, ForgeComponent comp, ref ItemPlacedEvent args)
    {
        if (HasComp<HeatedIngotComponent>(args.OtherEntity) && TryComp<FuelBurnerComponent>(uid, out var burner))
            _burner.UpdateAppearance(uid, burner);
    }

    private void OnItemRemoved(EntityUid uid, ForgeComponent comp, ref ItemRemovedEvent args)
    {
        if (HasComp<HeatedIngotComponent>(args.OtherEntity) && TryComp<FuelBurnerComponent>(uid, out var burner))
            _burner.UpdateAppearance(uid, burner);
    }

    private void HeatIngotsOnForge(FuelBurnerComponent burner, ItemPlacerComponent placer, float frameTime)
    {
        foreach (var ingotUid in placer.PlacedEntities)
        {
            if (!TryComp<HeatedComponent>(ingotUid, out var heated))
                continue;

            _burner.HeatObject(heated, burner.CurrentTemperature, 0.1f, frameTime);
        }
    }

    #endregion

    #region Ingots

    private bool IsOnHeater(EntityUid uid)
    {
        var forgeQuery = EntityQueryEnumerator<ForgeComponent, FuelBurnerComponent, ItemPlacerComponent>();
        while (forgeQuery.MoveNext(out _, out var burner, out var placer))
        {
            var placed = placer.PlacedEntities.ToList();
            if (burner.Ignited && placed.Contains(uid))
                return true;
        }

        return false;
    }

    private void UpdateIngotAppearance(EntityUid uid, HeatedComponent heated)
    {
        bool isHeated = heated.CurrentTemperature > 700;
        Color color = heated.CurrentTemperature switch
        {
            < 700 => Color.Transparent,
            >= 700 and < 800 => Color.DarkRed,
            >= 800 and < 900 => Color.Red,
            >= 900 and < 1000 => Color.FromHex("#c10020"),
            >= 1000 and < 1100 => Color.OrangeRed,
            >= 1100 and < 1200 => Color.LightYellow,
            >= 1200 => Color.WhiteSmoke,
            _ => Color.WhiteSmoke
        };

        if (TryComp<PointLightComponent>(uid, out var light))
        {
            if (isHeated != light.Enabled)
                _light.SetEnabled(uid, isHeated);

            if (light.Color != color)
                _light.SetColor(uid, color);
        }

        if (_appearance.TryGetData<Color>(uid, IngotVisuals.Color, out var currentColor))
        {
            if (currentColor != color)
                _appearance.SetData(uid, IngotVisuals.Color, color);
        }

        if (_appearance.TryGetData<bool>(uid, IngotVisuals.Glow, out var currentGlow))
        {
            if (currentGlow != isHeated)
                _appearance.SetData(uid, IngotVisuals.Glow, isHeated);
        }
    }

    #endregion
}
