using Robust.Shared.Audio;

namespace Content.Shared.Blacksmithing.Components;

[RegisterComponent]
public sealed partial class ForgingHammerComponent : Component
{
    [DataField("strikeTypes")]
    public List<StrikeType> AvailableStrikes = new()
    {
        StrikeType.Light,
        StrikeType.Medium,
        StrikeType.Heavy
    };

    [DataField("staminaCost")]
    public Dictionary<StrikeType, float> StaminaCost = new()
    {
        { StrikeType.Light, 0f },
        { StrikeType.Medium, 5f },
        { StrikeType.Heavy, 10f }
    };

    [DataField("currentStrikeIndex")]
    public int CurrentStrikeIndex = 0;

    [ViewVariables]
    public StrikeType CurrentStrike => AvailableStrikes[CurrentStrikeIndex];

    public SoundSpecifier Sounds = new SoundCollectionSpecifier("HummerStrikes");
}
