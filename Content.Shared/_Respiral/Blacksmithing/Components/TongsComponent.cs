using Robust.Shared.Containers;

namespace Content.Shared.Blacksmithing.Components;

[RegisterComponent]
public sealed partial class TongsComponent : Component
{
    public const string TongsContainerName = "tongs";
    public Container TongsContainer = new();

    [ViewVariables]
    public EntityUid? HeldIngot;
}
