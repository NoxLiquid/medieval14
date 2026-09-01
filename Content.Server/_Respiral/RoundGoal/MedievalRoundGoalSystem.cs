using System.Linq;
using Content.Server.Fax;
using Content.Server.GameTicking;
using Content.Shared._Respiral.RoundGoal;
using Content.Shared.Fax.Components;
using Content.Shared.Paper;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Respiral.RoundGoal;

/// <summary>
/// Respiral: в начале раунда выбирает случайную <see cref="MedievalRoundGoalPrototype"/>
/// и «телепортирует» её свитком на все столы телепортации, помеченные
/// <see cref="MedievalRoundGoalReceiverComponent"/>.
/// </summary>
public sealed partial class MedievalRoundGoalSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private FaxSystem _fax = default!;

    private static readonly Color StampColor = Color.FromHex("#8B4513");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnJobsAssigned);
    }

    private void OnJobsAssigned(RulePlayerJobsAssignedEvent ev)
    {
        var goals = _prototype.EnumeratePrototypes<MedievalRoundGoalPrototype>().ToList();
        if (goals.Count == 0)
            return;

        SendGoal(_random.Pick(goals));
    }

    /// <summary>
    /// Рассылает выбранную цель свитком на все столы-приёмники.
    /// </summary>
    public void SendGoal(MedievalRoundGoalPrototype goal)
    {
        var printout = new FaxPrintout(
            Loc.GetString(goal.Text),
            Loc.GetString("medieval-round-goal-paper-name"),
            prototypeId: "MedievalPaper",
            stampState: "paper_stamp-generic",
            stampedBy: new List<StampDisplayInfo>
            {
                new()
                {
                    StampedName = Loc.GetString("medieval-round-goal-stamp"),
                    StampedColor = StampColor,
                },
            });

        var query = EntityQueryEnumerator<MedievalRoundGoalReceiverComponent, FaxMachineComponent>();
        while (query.MoveNext(out var uid, out _, out var fax))
        {
            _fax.Receive(uid, printout, null, fax);
        }
    }
}
