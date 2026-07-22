using Content.Server._Respiral.Fluids.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Maps;
using Robust.Shared.Timing;

namespace Content.Server._Respiral.Fluids;

/// <summary>
/// Respiral: впитывание жидкости из луж тайлами с <see cref="ContentTileDefinition.Absorbent"/> = true.
/// Тайл вытягивает фиксированный объём из всего раствора лужи (все реагенты, независимо от испаряемости),
/// пропорционально составу.
///
/// Оптимизация: тайл под лужей проверяется один раз при её появлении (<see cref="MapInitEvent"/>),
/// впитывающие лужи помечаются <see cref="TileAbsorbingComponent"/>, а тик идёт только по помеченным.
/// </summary>
public sealed partial class PuddleAbsorptionSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    // Как часто впитывающий тайл вытягивает жидкость
    private static readonly TimeSpan AbsorptionCooldown = TimeSpan.FromSeconds(2);

    // Сколько единиц раствора тайл впитывает за секунду
    private static readonly FixedPoint2 AbsorptionRate = FixedPoint2.New(3);

    // Имя раствора у лужи — совпадает с PuddleComponent.SolutionName по умолчанию
    private const string PuddleSolutionName = "puddle";

    private TimeSpan _nextTick;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PuddleComponent, MapInitEvent>(OnPuddleMapInit);
    }

    // Один раз при появлении лужи проверяем тайл и, если он впитывающий, помечаем лужу маркером.
    // Лужи анкорятся к тайлу и не двигаются, поэтому повторная проверка каждый тик не нужна.
    // Проблема: если тайл под лужей поменяют, то она всё равно продолжит испаряться.
    private void OnPuddleMapInit(Entity<PuddleComponent> ent, ref MapInitEvent args)
    {
        if (_turf.GetTileRef(Transform(ent).Coordinates) is not { } tileRef)
            return;

        if (!_turf.GetContentTileDefinition(tileRef).Absorbent)
            return;

        AddComp<TileAbsorbingComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextTick)
            return;

        _nextTick = _timing.CurTime + AbsorptionCooldown;

        var query = EntityQueryEnumerator<TileAbsorbingComponent, PuddleComponent>();
        while (query.MoveNext(out var uid, out _, out _))
        {
            if (!_solutionContainer.TryGetSolution(uid, PuddleSolutionName, out var soln, out var solution))
                continue;

            if (solution.Volume <= FixedPoint2.Zero)
                continue;

            // SplitSolution убирает объём из всех реагентов пропорционально их доле в луже
            solution.SplitSolution(AbsorptionRate);

            // UpdateChemicals поднимет SolutionChangedEvent, а SharedPuddleSystem сам удалит пустую лужу
            _solutionContainer.UpdateChemicals(soln.Value);
        }
    }
}
