using Content.Server._Respiral.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Shared._Respiral.Mount;

namespace Content.Server._Respiral.Mount;

public sealed partial class MountSystem : SharedMountSystem
{
    [Dependency] private NPCSystem _npc = default!;
    [Dependency] private NPCSteeringSystem _steering = default!;

    protected override void OnRiderMounted(EntityUid mount, EntityUid rider)
    {
        _npc.SleepNPC(mount);

        _steering.Unregister(mount);

        EnsureComp<NPCIgnoringOptimizeComponent>(mount);
    }

    protected override void OnRiderDismounted(EntityUid mount, EntityUid rider)
    {
        RemComp<NPCIgnoringOptimizeComponent>(mount);
        _npc.WakeNPC(mount);
    }
}
