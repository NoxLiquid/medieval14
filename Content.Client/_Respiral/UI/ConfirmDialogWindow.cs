using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._Respiral.UI;

/// <summary>
///     Простое окно подтверждения с кнопками «Да» и «Нет».
/// </summary>
public sealed class ConfirmDialogWindow : DefaultWindow
{
    /// <summary>Вызывается при нажатии кнопки подтверждения.</summary>
    public event Action? OnConfirmed;

    /// <summary>Вызывается при нажатии кнопки отказа.</summary>
    public event Action? OnDeclined;

    public ConfirmDialogWindow(string title, string message, string yesText, string noText)
    {
        Title = title;

        var yesButton = new Button { Text = yesText };
        var noButton = new Button { Text = noText };

        yesButton.OnPressed += _ => OnConfirmed?.Invoke();
        noButton.OnPressed += _ => OnDeclined?.Invoke();

        Contents.AddChild(new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Children =
            {
                new Label
                {
                    Text = message,
                    HorizontalAlignment = HAlignment.Center,
                },
                new Control { MinSize = new Vector2(0, 20) },
                new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Align = AlignMode.Center,
                    Children =
                    {
                        yesButton,
                        new Control { MinSize = new Vector2(20, 0) },
                        noButton,
                    },
                },
            },
        });
    }
}
