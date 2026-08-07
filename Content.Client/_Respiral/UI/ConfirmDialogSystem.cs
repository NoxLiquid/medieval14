using Content.Shared._Respiral.UI;
using Robust.Client.Graphics;

namespace Content.Client._Respiral.UI;

/// <summary>
///     Клиентская часть окна подтверждения «Да/Нет»: открывает окно по запросу сервера
///     и отправляет обратно результат нажатия. Пара к серверному <c>ConfirmDialogSystem</c>.
/// </summary>
public sealed partial class ConfirmDialogSystem : EntitySystem
{
    [Dependency] private IClyde _clyde = default!;

    public override void Initialize()
    {
        SubscribeNetworkEvent<ConfirmDialogOpenEvent>(OnOpen);
    }

    private void OnOpen(ConfirmDialogOpenEvent ev)
    {
        var window = new ConfirmDialogWindow(ev.Title, ev.Message, ev.YesText, ev.NoText);
        var answered = false;

        window.OnConfirmed += () =>
        {
            answered = true;
            RaiseNetworkEvent(new ConfirmDialogResponseEvent(ev.DialogId, true));
            window.Close();
        };

        window.OnDeclined += () =>
        {
            answered = true;
            RaiseNetworkEvent(new ConfirmDialogResponseEvent(ev.DialogId, false));
            window.Close();
        };

        // Закрытие окна без выбора — отказ.
        window.OnClose += () =>
        {
            if (!answered)
                RaiseNetworkEvent(new ConfirmDialogResponseEvent(ev.DialogId, false));
        };

        _clyde.RequestWindowAttention();
        window.OpenCentered();
    }
}
