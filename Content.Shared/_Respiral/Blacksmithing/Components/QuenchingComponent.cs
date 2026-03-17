namespace Content.Shared._Respiral.Blacksmithing.Components;

[RegisterComponent]
public sealed partial class QuenchingComponent : Component
{
    /// <summary>
    /// Hey, make sure to change this to your <see cref="SolutionContainerManagerComponent"/> solutions!
    /// </summary>
    [DataField]
    public string Solution = "drink";

    [DataField]
    public string CoolingReagent = "Water";

    [DataField]
    public float ReagentAmount = 5f;
}
