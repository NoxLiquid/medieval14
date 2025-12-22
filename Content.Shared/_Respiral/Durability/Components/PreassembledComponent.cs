using Robust.Shared.Prototypes;

namespace Content.Shared.Durability;

[RegisterComponent]
[Access(typeof(SharedDurabilitySystem))]
public sealed partial class PreassembledComponent : Component
{
    /// <summary>
    /// Mapping of part slot names to lists of possible part prototypes to spawn for this preassembled item.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, List<EntProtoId>> Parts = new();

    /// <summary>
    /// If true, spawned parts will start with reduced (randomized) current durability.
    /// </summary>
    [DataField] public bool Worned = false;

    /// <summary>
    /// Range of random multipliers used to reduce spawned parts' current durability when <see cref="Worned"/> is true.
    /// The Max should be slightly higher than the ceiling value that is not included.
    /// </summary>
    public (float Min, float Max) WornedGap { get; private set; } = (0.05f, 0.96f);
}
