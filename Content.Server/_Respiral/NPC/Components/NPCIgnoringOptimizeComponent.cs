using Content.Server._Respiral.NPC.Systems;
using Content.Server.NPC.Systems;

namespace Content.Server._Respiral.NPC.Components;

[RegisterComponent, Access(typeof(NPCOptimizationSystem))]
public sealed partial class NPCIgnoringOptimizeComponent : Component;
