using Content.Server._Respiral.NPC.Systems;
using Content.Server.NPC.Systems;

namespace Content.Server._Respiral.NPC.Components;

[RegisterComponent, Access(typeof(NPCAggressionSystem))]
public sealed partial class NPCAggressionComponent : Component
{
    /// <summary>
    /// HTN blackboard key for the target entity
    /// </summary>
    [DataField]
    public string TargetKey = "Target";
}
