using Content.Shared._Respiral.UI;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._Respiral.UI;

/// <summary>
///     Показывает клиенту окно подтверждения «Да/Нет» и вызывает переданные обработчики нажатий.
///     Аналог <see cref="Content.Server.Administration.QuickDialogSystem"/>, но для простого выбора
///     да/нет через колбэки: вызываешь <see cref="OpenConfirmDialog"/> и обрабатываешь ответ в них.
/// </summary>
public sealed class ConfirmDialogSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    /// <summary>Обработчики «да»/«нет» по идентификатору открытого окна.</summary>
    private readonly Dictionary<int, (Action Yes, Action No)> _openDialogs = new();
    private readonly Dictionary<NetUserId, List<int>> _openDialogsByUser = new();

    private int _nextDialogId = 1;

    public override void Initialize()
    {
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

        SubscribeNetworkEvent<ConfirmDialogResponseEvent>(OnResponse);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    /// <summary>
    ///     Открывает у клиента окно «Да/Нет». <paramref name="yesAction"/> вызывается при подтверждении,
    ///     <paramref name="noAction"/> — при отказе, закрытии окна или отключении игрока.
    /// </summary>
    /// <param name="session">Клиент, которому показать окно.</param>
    /// <param name="title">Заголовок окна.</param>
    /// <param name="message">Текст запроса.</param>
    /// <param name="yesAction">Действие при нажатии «Да».</param>
    /// <param name="noAction">Действие при нажатии «Нет»/закрытии.</param>
    /// <param name="yesText">Надпись кнопки подтверждения (по умолчанию — «Да»).</param>
    /// <param name="noText">Надпись кнопки отказа (по умолчанию — «Нет»).</param>
    [PublicAPI]
    public void OpenConfirmDialog(ICommonSession session, string title, string message, Action yesAction,
        Action? noAction = null, string? yesText = null, string? noText = null)
    {
        var id = _nextDialogId++;

        RaiseNetworkEvent(new ConfirmDialogOpenEvent(
                id,
                title,
                message,
                yesText ?? Loc.GetString("confirm-dialog-yes"),
                noText ?? Loc.GetString("confirm-dialog-no")),
            session);

        _openDialogs[id] = (yesAction, noAction ?? (() => { }));

        if (!_openDialogsByUser.TryGetValue(session.UserId, out var userDialogs))
        {
            userDialogs = new List<int>();
            _openDialogsByUser[session.UserId] = userDialogs;
        }

        userDialogs.Add(id);
    }

    private void OnResponse(ConfirmDialogResponseEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.UserId;

        if (!_openDialogs.TryGetValue(msg.DialogId, out var actions)
            || !_openDialogsByUser.TryGetValue(user, out var userDialogs)
            || !userDialogs.Contains(msg.DialogId))
        {
            args.SenderSession.Channel.Disconnect($"Replied with invalid confirm dialog id {msg.DialogId}.");
            return;
        }

        if (msg.Confirmed)
            actions.Yes.Invoke();
        else
            actions.No.Invoke();

        _openDialogs.Remove(msg.DialogId);
        userDialogs.Remove(msg.DialogId);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.Disconnected && e.NewStatus != SessionStatus.Zombie)
            return;

        if (!_openDialogsByUser.Remove(e.Session.UserId, out var dialogs))
            return;

        // Отключение считаем отказом от всех открытых окон.
        foreach (var id in dialogs)
        {
            if (_openDialogs.Remove(id, out var actions))
                actions.No.Invoke();
        }
    }
}
