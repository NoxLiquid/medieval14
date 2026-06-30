namespace Content.Shared._Respiral.Identity.Components;

/// <summary>
/// Маркер для клиентского визуализатора грамоты: меняет состояние базового спрайта,
/// когда грамоту разворачивают (предъявляют всем). Состояния настраиваются в прототипе.
/// </summary>
[RegisterComponent]
public sealed partial class MedievalCredentialVisualizerComponent : Component
{
    /// <summary>Состояние спрайта свёрнутой грамоты (обложка).</summary>
    [DataField]
    public string ClosedState = "passport_close";

    /// <summary>Состояние спрайта развёрнутой (предъявляемой) грамоты.</summary>
    [DataField]
    public string OpenState = "passport_open";
}
