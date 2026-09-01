using Content.Client.UserInterface.Systems.Gameplay;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._Respiral.Respawn;

public sealed class MedievalRespawnUiController : UIController
{
    public override void Initialize()
    {
        base.Initialize();

        var load = UIManager.GetUIController<GameplayStateLoadController>();
        load.OnScreenLoad += OnScreenLoad;
    }

    private void OnScreenLoad()
    {
        UIManager.ActiveScreen?.GetOrAddWidget<MedievalRespawnWidget>();
    }
}
