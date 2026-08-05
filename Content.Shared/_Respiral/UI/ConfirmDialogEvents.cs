using Robust.Shared.Serialization;

namespace Content.Shared._Respiral.UI;

/// <summary>
///     Сетевое событие: сервер просит клиента открыть окно подтверждения «Да/Нет».
/// </summary>
[Serializable, NetSerializable]
public sealed class ConfirmDialogOpenEvent : EntityEventArgs
{
    /// <summary>Внутренний идентификатор окна.</summary>
    public int DialogId;

    /// <summary>Заголовок окна.</summary>
    public string Title;

    /// <summary>Текст запроса.</summary>
    public string Message;

    /// <summary>Надпись на кнопке подтверждения.</summary>
    public string YesText;

    /// <summary>Надпись на кнопке отказа.</summary>
    public string NoText;

    public ConfirmDialogOpenEvent(int dialogId, string title, string message, string yesText, string noText)
    {
        DialogId = dialogId;
        Title = title;
        Message = message;
        YesText = yesText;
        NoText = noText;
    }
}

/// <summary>
///     Сетевое событие: ответ клиента на окно подтверждения.
/// </summary>
[Serializable, NetSerializable]
public sealed class ConfirmDialogResponseEvent : EntityEventArgs
{
    /// <summary>Внутренний идентификатор окна.</summary>
    public int DialogId;

    /// <summary>Нажата ли кнопка подтверждения (иначе — отказ/закрытие).</summary>
    public bool Confirmed;

    public ConfirmDialogResponseEvent(int dialogId, bool confirmed)
    {
        DialogId = dialogId;
        Confirmed = confirmed;
    }
}
