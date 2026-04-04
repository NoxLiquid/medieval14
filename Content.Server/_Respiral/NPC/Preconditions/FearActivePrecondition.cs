using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Robust.Shared.Timing;

namespace Content._Respiral.NPC.Preconditions;

public sealed partial class FearActivePrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    [DataField]
    public string Key = MedievalNPCBlackboard.FearEndTime;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        return blackboard.TryGetValue<TimeSpan>(Key, out var fearEndTime, _entManager) && fearEndTime > _timing.CurTime;
    }
}