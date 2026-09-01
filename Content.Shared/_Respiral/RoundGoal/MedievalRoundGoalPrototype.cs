using Robust.Shared.Prototypes;

namespace Content.Shared._Respiral.RoundGoal;

/// <summary>
/// Respiral: цель раунда — задача поселению на текущий цикл. В начале раунда одна такая
/// цель «телепортируется» свитком на все столы телепортации, помеченные
/// <c>MedievalRoundGoalReceiverComponent</c>.
/// </summary>
[Prototype]
public sealed partial class MedievalRoundGoalPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Текст цели, который окажется в свитке.
    /// </summary>
    [DataField(required: true)]
    public LocId Text;
}
