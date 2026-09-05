using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Shared._Respiral.Respawn;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Ghost.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Respiral.Respawn;

/// <summary>
/// Таймер респавна для призраков. Таймер сбрасывается, если игрок вернулся в живое тело
/// (<see cref="MobState.Alive"/>).
/// </summary>
public sealed partial class MedievalRespawnSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private StationSystem _station = default!;

    private static readonly TimeSpan RespawnDelay = TimeSpan.FromMinutes(15);

    // Момент доступности респавна на игрока. Живёт, пока игрок мёртв/призрак; удаляется при возвращении в живое тело.
    private readonly Dictionary<NetUserId, TimeSpan> _respawnAt = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostComponent, PlayerAttachedEvent>(OnGhostAttached);
        SubscribeLocalEvent<MobStateComponent, PlayerAttachedEvent>(OnMobAttached);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnCleanup);
        SubscribeNetworkEvent<MedievalGhostRespawnRequestEvent>(OnRespawnRequest);
    }

    // Игрок стал призраком — запускаем таймер (если ещё не запущен) и вешаем сетевой кэш на призрака.
    private void OnGhostAttached(EntityUid uid, GhostComponent component, PlayerAttachedEvent args)
    {
        var user = args.Player.UserId;
        if (!_respawnAt.TryGetValue(user, out var at))
        {
            at = _timing.CurTime + RespawnDelay;
            _respawnAt[user] = at;
        }

        var respawn = EnsureComp<MedievalGhostRespawnComponent>(uid);
        respawn.RespawnAt = at;
        Dirty(uid, respawn);
    }

    // Игрок вселился в живое тело — сброс таймера.
    private void OnMobAttached(EntityUid uid, MobStateComponent component, PlayerAttachedEvent args)
    {
        if (component.CurrentState == MobState.Alive)
            _respawnAt.Remove(args.Player.UserId);
    }

    // Тело игрока ожило (воскрешение на месте) — сброс таймера.
    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive && TryComp<ActorComponent>(args.Target, out var actor))
            _respawnAt.Remove(actor.PlayerSession.UserId);
    }

    private void OnCleanup(RoundRestartCleanupEvent ev)
    {
        _respawnAt.Clear();
    }

    private void OnRespawnRequest(MedievalGhostRespawnRequestEvent msg, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        if (!_respawnAt.TryGetValue(session.UserId, out var at) || _timing.CurTime < at)
            return;

        if (_station.GetStations().FirstOrNull() is not { } station)
            return;

        _respawnAt.Remove(session.UserId);
        _ticker.MakeJoinGame(session, station, silent: true);
    }
}
