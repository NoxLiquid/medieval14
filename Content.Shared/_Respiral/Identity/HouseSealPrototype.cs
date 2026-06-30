using Robust.Shared.Prototypes;

namespace Content.Shared._Respiral.Identity;

/// <summary>
/// Дом/канцелярия, чья печать заверяет грамоты. «Конкретный герб» — задел под
/// различение юрисдикций и отзыв грамот.
/// </summary>
[Prototype]
public sealed partial class HouseSealPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name = string.Empty;
}
