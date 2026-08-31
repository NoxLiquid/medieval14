using Content.Shared._Respiral.Respawn;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._Respiral.Respawn;

public sealed partial class MedievalRespawnWidget : UIWidget
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IEntityNetworkManager _net = default!;

    private static readonly Color Waiting = Color.FromHex("#8B2E2E");
    private static readonly Color Ready = Color.FromHex("#2E8B3E");

    private readonly Button _button;

    public MedievalRespawnWidget()
    {
        IoCManager.InjectDependencies(this);

        HorizontalAlignment = HAlignment.Center;
        VerticalAlignment = VAlignment.Top;
        Margin = new Thickness(0, 42, 0, 0); // под рядом стандартных ghost-кнопок
        MouseFilter = MouseFilterMode.Ignore;

        _button = new Button { HorizontalAlignment = HAlignment.Center, Visible = false };
        _button.OnPressed += _ => _net.SendSystemNetworkMessage(new MedievalGhostRespawnRequestEvent());
        AddChild(_button);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_players.LocalEntity is not { } player
            || !_entMan.TryGetComponent<MedievalGhostRespawnComponent>(player, out var respawn))
        {
            _button.Visible = false;
            return;
        }

        _button.Visible = true;
        var remaining = respawn.RespawnAt - _timing.CurTime;

        if (remaining > TimeSpan.Zero)
        {
            var text = $"{(int)remaining.TotalMinutes:D2}:{remaining.Seconds:D2}";
            _button.Text = Loc.GetString("medieval-respawn-button-waiting", ("time", text));
            _button.Disabled = true;
            _button.ModulateSelfOverride = Waiting;
        }
        else
        {
            _button.Text = Loc.GetString("medieval-respawn-button-ready");
            _button.Disabled = false;
            _button.ModulateSelfOverride = Ready;
        }
    }
}
