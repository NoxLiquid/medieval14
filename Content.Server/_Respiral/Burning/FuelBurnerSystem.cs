using Content.Server.Audio;
using Content.Server.Stack;
using Content.Shared._Respiral.Burning;
using Content.Shared._Respiral.Burning.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands;
using Content.Shared.IgnitionSource;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Pulling.Events;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Respiral.Burning;

public sealed partial class FuelBurnerSystem : EntitySystem
{
    [Dependency] private AmbientSoundSystem _ambient = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private FuelSystem _fuel = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SharedPointLightSystem _light = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StackSystem _stack = default!;

    private static readonly ProtoId<DamageTypePrototype> Damage = "Heat";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FuelBurnerComponent, AfterInteractUsingEvent>(OnBurnerInteractUsing);
        SubscribeLocalEvent<FuelBurnerComponent, MapInitEvent>(OnBurnerInit);

        SubscribeLocalEvent<HeatedComponent, GotEquippedHandEvent>(OnHeatedEquipped);
        SubscribeLocalEvent<HeatedComponent, BeingPulledAttemptEvent>(OnHeatedPulledAttempt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FuelBurnerComponent>();
        while (query.MoveNext(out var uid, out var burner))
        {
            UpdateBurner(uid, burner, frameTime);
        }
    }

    private void OnBurnerInit(EntityUid uid, FuelBurnerComponent component, MapInitEvent args)
    {
        UpdateAppearance(uid, component);
    }

    public void OnBurnerInteractUsing(EntityUid uid, FuelBurnerComponent burner, AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        var used = args.Used;
        if (!burner.Ignited && TryComp<IgnitionSourceComponent>(used, out var ignition) && ignition.Ignited)
        {
            if (burner.FuelAmount > 0f)
            {
                IgniteBurner(uid, burner);
                args.Handled = true;
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("burner-needs-fuel"), args.User, args.User);
            }
            return;
        }

        if (TryComp<FuelComponent>(used, out var fuelComp) && TryComp<StackComponent>(used, out var stack))
        {
            var space = burner.FuelCapacity - burner.FuelAmount;
            var take = Math.Min(stack.Count, (int)space);

            if (take <= 0)
            {
                _popup.PopupEntity(Loc.GetString("burner-full", ("burner", Name(uid))), args.User, args.User);
                return;
            }

            if (AddFuel(uid, fuelComp.FuelType, take, burner))
            {
                _popup.PopupEntity(Loc.GetString("burner-fuel-added", ("amount", take), ("fuel", Name(used))),
                    args.User, args.User);

                if (stack.Count == take)
                    QueueDel(used);
                else
                    _stack.SetCount(used, stack.Count - take, stack);

                args.Handled = true;
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("burner-full", ("burner", Name(uid))), args.User, args.User);
            }
            return;
        }
    }

    public void UpdateBurner(EntityUid uid, FuelBurnerComponent burner, float frameTime)
    {
        CheckBurnerIgnition(uid, burner);

        if (!burner.Ignited || burner.FuelAmount <= 0f)
        {
            burner.CurrentTemperature = Math.Max(0, burner.CurrentTemperature - burner.TempDecayRate * frameTime);
            UpdateAppearance(uid, burner);
            return;
        }

        var fuelEfficiency = burner.FuelEfficiency;
        var tempRise = burner.TempRiseRate * fuelEfficiency * frameTime;
        tempRise += burner.Ventilation * 10f * frameTime;

        burner.CurrentTemperature = Math.Min(burner.MaxTemperature, burner.CurrentTemperature + tempRise);
        burner.FuelAmount -= burner.BurnRate * frameTime;

        if (burner.Ignited && burner.CurrentTemperature > 1150f
            && _gameTiming.CurTime - burner.LastDamageTime > TimeSpan.FromSeconds(1))
        {
            if (TryComp<DamageableBurnerComponent>(uid, out var damageable))
            {
                damageable.Condition -= burner.DamageRate;
                burner.LastDamageTime = _gameTiming.CurTime;

                if (damageable.Condition <= 0f)
                {
                    QueueDel(uid);
                    return;
                }
            }
        }

        if (burner.PassiveVentilation && burner.Ignited)
        {
            burner.Ventilation += burner.PassiveVentilationRate * frameTime;
        }

        burner.Ventilation = Math.Max(0, burner.Ventilation - frameTime * 2f);
        UpdateAppearance(uid, burner);
    }

    public void HeatObject(HeatedComponent heated, float sourceTemperature, float transferRate, float frameTime)
    {
        var temperatureDifference = sourceTemperature - heated.CurrentTemperature;
        if (temperatureDifference <= 0)
            return;

        var heatTransfer = temperatureDifference * transferRate * frameTime;
        heated.CurrentTemperature = Math.Min(sourceTemperature, heated.CurrentTemperature + heatTransfer);
    }

    public void CoolObject(EntityUid uid, HeatedComponent heated, float frameTime)
    {
        heated.CurrentTemperature = Math.Max(0, heated.CurrentTemperature - heated.CoolingRate * frameTime);
    }

    public void IgniteBurner(EntityUid uid, FuelBurnerComponent burner)
    {
        burner.Ignited = true;
        UpdateAppearance(uid, burner);
    }

    private void CheckBurnerIgnition(EntityUid uid, FuelBurnerComponent burner)
    {
        if (burner.Ignited && burner.FuelAmount <= 0f)
        {
            burner.Ignited = false;
            burner.Ventilation = 0f;
            UpdateAppearance(uid, burner);
        }
    }

    public bool AddFuel(EntityUid burnerUid, FuelType fuel, float amount, FuelBurnerComponent? burnerComp = null)
    {
        if (!Resolve(burnerUid, ref burnerComp))
            return false;

        var space = burnerComp.FuelCapacity - burnerComp.FuelAmount;
        if (space <= 0) return false;

        amount = Math.Min(amount, space);
        if (burnerComp.FuelAmount > 0 && burnerComp.FuelType != null)
        {
            if (!AreFuelTypesEqual(burnerComp.FuelType.Value, fuel))
            {
                burnerComp.FuelType = _fuel.MixFuels(
                    burnerComp.FuelType.Value, fuel,
                    burnerComp.FuelAmount, amount
                );
            }
        }
        else
        {
            burnerComp.FuelType = fuel;
        }

        burnerComp.FuelAmount += amount;
        UpdateAppearance(burnerUid, burnerComp);
        return true;
    }

    private bool AreFuelTypesEqual(FuelType a, FuelType b)
    {
        return MathHelper.CloseTo(a.BaseTemp, b.BaseTemp) &&
            MathHelper.CloseTo(a.MaxTemp, b.MaxTemp);
    }

    public void UpdateAppearance(EntityUid uid, FuelBurnerComponent burner)
    {
        bool active = burner.Ignited && burner.FuelAmount > 0f;
        float temperatureRatio = burner.CurrentTemperature / burner.MaxTemperature;

        _appearance.SetData(uid, BurnerVisuals.Active, active);

        _ambient.SetAmbience(uid, active);

        if (TryComp<PointLightComponent>(uid, out var light))
        {
            if (active != light.Enabled)
                _light.SetEnabled(uid, active);

            if (active)
            {
                var color = temperatureRatio switch
                {
                    < 0.3f => Color.FromHex("#ff8c00"),
                    < 0.6f => Color.FromHex("#ff4500"),
                    < 0.8f => Color.FromHex("#ff0000"),
                    _ => Color.FromHex("#ffff00")
                };

                if (light.Color != color)
                    _light.SetColor(uid, color);
            }
        }
    }

    private void OnHeatedEquipped(EntityUid uid, HeatedComponent comp, GotEquippedHandEvent args)
    {
        if (comp.CurrentTemperature < 60)
            return;

        args.Handled = true;
        DoHeatDamage(args.User);
    }

    private void OnHeatedPulledAttempt(EntityUid uid, HeatedComponent comp, BeingPulledAttemptEvent args)
    {
        if (comp.CurrentTemperature < 60)
            return;

        args.Cancel();
        DoHeatDamage(args.Puller);
    }

    private void DoHeatDamage(EntityUid user)
    {
        var damage = new DamageSpecifier { DamageDict = { { Damage, 5 } } };
        _damage.TryChangeDamage(user, damage, true);

        _audio.PlayEntity(new SoundPathSpecifier("/Audio/Effects/lightburn.ogg"), user, user);
    }
}
