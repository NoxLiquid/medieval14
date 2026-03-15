using Content.Server.EUI;
using Content.Shared._Respiral.Skills;
using Content.Shared._Respiral.Skills.Events;
using Content.Shared.Eui;

namespace Content.Server._Respiral.Skill;

public sealed class SkillsListEui : BaseEui
{
    private readonly string _targetName;
    private readonly List<SkillInfo> _skills;

    public SkillsListEui(string targetName, List<SkillInfo> skills)
    {
        _targetName = targetName;
        _skills = skills;
    }

    public override void Opened()
    {
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        return new SkillsListEuiState(_targetName, _skills);
    }
}
