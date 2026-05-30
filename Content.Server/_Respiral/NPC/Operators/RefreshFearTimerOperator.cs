using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared._Respiral.TimeWindow;

namespace Content._Respiral.NPC.Operators;

public sealed partial class RefreshFearTimerOperator : HTNOperator
{
    private TimedWindowSystem _timedWindow = default!;

    [DataField]
    public string Key = MedievalNPCBlackboard.FearEndTime;

    [DataField]
    public TimedWindow TimedWindow = MedievalNPCDefaultsSettings.FearWindowDuration.Clone();

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _timedWindow = sysManager.GetEntitySystem<TimedWindowSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard, CancellationToken cancelToken)
    {
        return (true, new Dictionary<string, object>
        {
            { Key, _timedWindow.GetRandomTime(TimedWindow) },
        });
    }
}