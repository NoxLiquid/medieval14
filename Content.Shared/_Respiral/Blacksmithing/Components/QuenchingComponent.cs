namespace Content.Shared.Blacksmithing.Components;

[RegisterComponent]
public sealed partial class QuenchingComponent : Component
{
    /// <summary>
    /// Hey, make sure to change this to your <see cref="SolutionContainerManagerComponent"/> solutions!
    /// </summary>
    /// uwu
    [DataField(required: true)]
    public string Solution = "uwu";

    [DataField]
    public string CoolingReagent = "Water";

    [DataField]
    public float ReagentAmount = 5f;
}
