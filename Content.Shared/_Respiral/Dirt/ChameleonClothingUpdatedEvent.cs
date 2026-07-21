using Robust.Shared.Prototypes;

namespace Content.Shared._Respiral.Dirt;

/// <summary>Хамелеон обновил вид под прототип <paramref name="Proto"/> (точка расширения для грязи).</summary>
[ByRefEvent]
public readonly record struct ChameleonClothingUpdatedEvent(EntityPrototype Proto);
