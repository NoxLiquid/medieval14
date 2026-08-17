using Content.Shared.Hands.Components;

namespace Content.Client._Respiral.Inhand;

[RegisterComponent]
public sealed partial class MedievalIconInhandComponent : Component
{
    public readonly Dictionary<HandLocation, HandIconFallback> Hands = new();

    public Direction LastDirection = Direction.Invalid;
}

public sealed class HandIconFallback
{
    public readonly List<string> OverKeys = new();
    public readonly List<string> UnderKeys = new();
    public readonly List<PrototypeLayerData> UnderData = new();
}
