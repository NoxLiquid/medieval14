using Content.Server.EUI;
using Content.Shared._Respiral.Skill.Events;
using Content.Shared.Eui;

namespace Content.Server._Respiral.Skill;

public sealed class SkillShareRequestEui : BaseEui
{
    private readonly EntityUid _target;
    private readonly EntityUid _requester;
    private readonly SkillShareSystem _system;
    private readonly string _requesterName;

    public SkillShareRequestEui(EntityUid target, EntityUid requester, SkillShareSystem system, string requesterName)
    {
        _target = target;
        _requester = requester;
        _system = system;
        _requesterName = requesterName;
    }

    public override void Opened()
    {
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        return new SkillShareRequestEuiState(_requesterName);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not SkillShareResponseMessage response)
        {
            Close();
            return;
        }

        _system.HandleSkillShareResponse(_target, _requester, Player, response.Accepted);
        Close();
    }
}
