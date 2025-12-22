using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Durability;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDurabilitySystem))]
public sealed partial class DurabilityComponent : Component
{
    public const string PartsContainerName = "item_parts";
    [ViewVariables] public Container PartsContainer = default!;

    /// <summary>
    /// Current durability value; when it reaches 0 the item is broken.
    /// </summary>
    [DataField]
    public float CurrentDurability = 100;

    /// <summary>
    /// Maximum durability value for this item.
    /// </summary>
    [DataField]
    public float MaxDurability = 100;

    /// <summary>
    /// Ratio used to convert incoming damage into durability loss (used for armor/mitigation).
    /// </summary>
    [DataField]
    public float DamageToDurabilityRatio = 0.1f;

    /// <summary>
    /// Probability (0..1) that a single event will apply durability loss.
    /// </summary>
    [DataField]
    public float DamageChance = 1f;

    /// <summary>
    /// Whether the item should lose durability when used (melee hits, throwing, tool usage, etc).
    /// </summary>
    [DataField]
    public bool DegradeOnUse = true;

    /// <summary>
    /// Base durability loss applied on a single use (also acts as a minimum loss when damage is converted).
    /// </summary>
    [DataField]
    public float CurrentDurabilityLoss = 1;

    /// <summary>
    /// Minimum multiplier applied to durability loss when randomizing.
    /// </summary>
    [DataField]
    public float MinLossMultiplier = 0.5f;

    /// <summary>
    /// Maximum multiplier applied to durability loss when randomizing.
    /// </summary>
    [DataField]
    public float MaxLossMultiplier = 1.5f;

    /// <summary>
    /// If true, the entity will be deleted when its durability reaches zero.
    /// </summary>
    [DataField]
    public bool DestroyOnBreak = true;

    /// <summary>
    /// When spawning replacement objects on break, attempt to place them in the user's hands.
    /// </summary>
    [DataField]
    public bool LeaveInHands = false;

    /// <summary>
    /// If true, this item's durability is derived from contained part entities instead of a single value.
    /// </summary>
    [DataField]
    public bool UsePartsDurability = false;

    /// <summary>
    /// Optional list of prototype IDs to spawn when this item is destroyed (e.g., debris or replacement objects).
    /// </summary>
    [DataField]
    public List<EntProtoId>? DestroyObjects = default!;

    /// <summary>
    /// Sound played when the item or one of its parts breaks.
    /// </summary>
    [DataField]
    public SoundSpecifier? BreakSound;
}

[RegisterComponent]
[Access(typeof(SharedDurabilitySystem))]
public sealed partial class PartDurabilityComponent : Component
{
    /// <summary>
    /// Chance (0..1) that this part will take damage when the parent item receives durability loss.
    /// </summary>
    [DataField]
    public float PartDamageChance = 0.3f;

    /// <summary>
    /// Multiplier applied to the base durability loss when this part is selected to receive damage.
    /// </summary>
    [DataField]
    public float PartDamageMultiplier = 1.5f;

    /// <summary>
    /// If true, the destruction of this part will cause the entire parent item to be considered broken.
    /// </summary>
    [DataField("isCritical")]
    public bool IsCriticalPart;

    /// <summary>
    /// If true, this item can theoretically be picked up when all weapons are broken.
    /// </summary>
    [DataField]
    public bool CanPickable;
}
