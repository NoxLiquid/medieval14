using System.Linq;
using System.Numerics;
using Content.Server.Damage.Systems;
using Content.Shared._Respiral.Blacksmithing;
using Content.Shared._Respiral.Blacksmithing.Components;
using Content.Shared.Burning.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Respiral.Blacksmithing.Systems;

public sealed class ForgingSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StaminaSystem _stamina = default!;

    private const int MAXSTRIKES = 64; /// 2^6 too much

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnvilComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<AnvilComponent, ItemRemovedEvent>(OnItemRemoved);

        SubscribeLocalEvent<ForgingHammerComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<ForgingHammerComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ForgingHammerComponent, AfterInteractEvent>(OnHammerInteract);
    }

    private void OnItemPlaced(EntityUid uid, AnvilComponent comp, ref ItemPlacedEvent args)
    {
        if (HasComp<HeatedIngotComponent>(args.OtherEntity))
            EnsureComp<HeatedIngotPlacedComponent>(args.OtherEntity);
    }

    private void OnItemRemoved(EntityUid uid, AnvilComponent comp, ref ItemRemovedEvent args)
    {
        if (HasComp<HeatedIngotComponent>(args.OtherEntity))
            RemComp<HeatedIngotPlacedComponent>(args.OtherEntity);
    }

    private void OnGetVerb(EntityUid uid, ForgingHammerComponent comp, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        for (var i = 0; i < comp.AvailableStrikes.Count; i++)
        {
            var index = i;
            var strikeType = comp.AvailableStrikes[i];
            var strikeName = Loc.GetString($"forging-hammer-mode-{strikeType.ToString().ToLower()}");

            var verb = new Verb
            {
                Priority = 1,
                Category = VerbCategory.SelectType,
                Text = strikeName,
                Disabled = i == comp.CurrentStrikeIndex,
                DoContactInteraction = true,
                Act = () =>
                {
                    TrySetStrikeMode(comp, index, args.User);
                }
            };

            args.Verbs.Add(verb);
        }
    }

    private void OnUseInHand(EntityUid uid, ForgingHammerComponent comp, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        TryCycleStrikeMode(comp, args.User);
    }

    public void TryCycleStrikeMode(ForgingHammerComponent comp, EntityUid? user = null)
    {
        if (comp.AvailableStrikes.Count < 2)
            return;

        var index = (comp.CurrentStrikeIndex + 1) % comp.AvailableStrikes.Count;
        TrySetStrikeMode(comp, index, user);
    }

    public bool TrySetStrikeMode(ForgingHammerComponent comp, int index, EntityUid? user = null)
    {
        if (index < 0 || index >= comp.AvailableStrikes.Count)
            return false;

        comp.CurrentStrikeIndex = index;

        var strikeType = comp.CurrentStrike;
        var strikeName = Loc.GetString($"forging-hammer-mode-{strikeType.ToString().ToLower()}");

        if (user != null)
        {
            _popup.PopupEntity(Loc.GetString("forging-hammer-mode-set", ("mode", strikeName)),
                user.Value, user.Value);
        }

        return true;
    }

    private void OnHammerInteract(EntityUid uid, ForgingHammerComponent hammer, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        EntityUid? targetIngot = null;
        if (HasComp<AnvilComponent>(args.Target.Value))
        {
            var anvil = args.Target.Value;
            if (!TryComp<ItemPlacerComponent>(anvil, out var placer) || placer.PlacedEntities.Count == 0)
                return;

            targetIngot = placer.PlacedEntities.First();
        }
        else if (HasComp<HeatedIngotPlacedComponent>(args.Target.Value))
            targetIngot = args.Target.Value;

        if (targetIngot == null)
            return;

        var ingot = targetIngot.Value;
        if (!TryComp<BlacksmithingProcessComponent>(ingot, out var process)
            || !TryComp<HeatedComponent>(ingot, out var heated))
            return;

        if (process.StrikeSequence.Count >= MAXSTRIKES) // Stop hitting me!
            return;

        if (heated.CurrentTemperature < 700)
            return;

        if (heated.CurrentTemperature >= heated.OverheatThreshold)
        {
            QueueDel(ingot);
            return;
        }

        if (!_stamina.TryTakeStamina(args.User, hammer.StaminaCost[hammer.CurrentStrike]))
            return;

        PerformStrike(ingot, process, heated, hammer.CurrentStrike);
        _audio.PlayPvs(hammer.Sounds, ingot);

        args.Handled = true;
    }

    private void PerformStrike(EntityUid ingot, BlacksmithingProcessComponent process, HeatedComponent heated,
        StrikeType strikeType)
    {
        var currentTime = _gameTiming.CurTime;

        double timeFromPrevious = process.StrikeSequence.Count > 0
            ? (currentTime - process.LastStrikeTime).TotalSeconds : 1.0;

        var strikeData = new StrikeData
        {
            Type = strikeType,
            Time = currentTime,
            Temperature = heated.CurrentTemperature,
            TimeFromPrevious = timeFromPrevious
        };

        process.StrikeSequence.Add(strikeData);

        process.LastStrikeTime = currentTime;
        process.CurrentStage++;

        UpdateForgingAppearance(ingot, process);
    }

    private void UpdateForgingAppearance(EntityUid uid, BlacksmithingProcessComponent process)
    {
        if (!HasComp<HeatedIngotComponent>(uid))
            return;

        var maxStrikes = MAXSTRIKES / 2; // Less than the dividing ceiling

        var strikeCount = Math.Min(process.StrikeSequence.Count, maxStrikes);
        var deformationY = 1.0f - strikeCount / (float)maxStrikes * 0.4f;
        var deformationVector = new Vector2(1.0f, deformationY);

        _appearance.SetData(uid, IngotVisuals.Deformation, deformationVector);
    }
}
