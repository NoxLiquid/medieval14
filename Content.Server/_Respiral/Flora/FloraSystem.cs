using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Server._Respiral.Flora.Components;
using Content.Shared._Respiral.Flora;
using Content.Shared._Respiral.TimeWindow;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server._Respiral.Flora;

// Зачем нужна эта система?
// TODO нужна пояснительная записка или комментарий: для чего писался код.
public sealed partial class FloraSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedToolSystem _tool = default!;
    [Dependency] private TimedWindowSystem _timedWindow = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FloraComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FloraComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<FloraComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<FloraComponent, FloraHarvestDoAfterEvent>(OnHarvestDoAfter);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var floraQuery = EntityQueryEnumerator<FloraComponent>();
        while (floraQuery.MoveNext(out var uid, out var flora))
        {
            if (flora.IsGrown)
                continue;

            if (_timedWindow.IsExpired(flora.GrowthWindow))
                CompleteGrowth(uid, flora);
        }
    }

    private void OnMapInit(EntityUid uid, FloraComponent component, MapInitEvent args)
    {
        UpdateAppearance(uid, component);
    }

    private void OnInteractHand(EntityUid uid, FloraComponent component, InteractHandEvent args)
    {
        if (args.Handled || !component.IsGrown || component.SpecialTool != null)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(component.HarvestDuration),
            new FloraHarvestDoAfterEvent(), uid, uid, args.User)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = true,
            NeedHand = false
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnInteractUsing(EntityUid uid, FloraComponent component, InteractUsingEvent args)
    {
        if (args.Handled || !component.IsGrown)
            return;

        if (component.SpecialTool != null
            && !_tool.HasQuality(args.Used, component.SpecialTool))
        {
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(component.HarvestDuration),
            new FloraHarvestDoAfterEvent(), uid, uid, args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = true,
            NeedHand = false
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnHarvestDoAfter(EntityUid uid, FloraComponent component, FloraHarvestDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !component.IsGrown || args.Used == null)
            return;

        if (component.SpecialTool != null
            && args.Used.Value.Valid
            && !_tool.HasQuality(args.Used.Value, component.SpecialTool.Value))
        {
            return;
        }

        HarvestFlora(uid, component, args.User);
        args.Handled = true;
    }

    private void CompleteGrowth(EntityUid uid, FloraComponent component)
    {
        component.IsGrown = true;

        UpdateAppearance(uid, component);
        Dirty(uid, component);
    }

    private void HarvestFlora(EntityUid uid, FloraComponent component, EntityUid user)
    {
        var yieldCount = _random.Next(component.MinYield, component.MaxYield + 1);

        var spawnPos = Transform(uid).Coordinates;
        for (int i = 0; i < yieldCount; i++)
        {
            Spawn(component.HarvestPrototype, spawnPos);
        }

        component.IsGrown = false;
        _timedWindow.Reset(component.GrowthWindow);

        _audio.PlayPvs(component.HarvestSound, uid);

        UpdateAppearance(uid, component);
        Dirty(uid, component);
    }

    // избыточно дублировать на клиент, т.к. 100 мс. не повлияют на игровой опыт в данном случаи
    private void UpdateAppearance(EntityUid uid, FloraComponent component)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        var state = component.IsGrown ? FloraState.Grown : FloraState.Harvested;

        _appearance.SetData(uid, FloraVisuals.State, state, appearance);
    }
}
