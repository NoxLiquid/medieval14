using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Respiral.Skill.Events;

/// <summary>
/// EUI state for showing the skills list of another player.
/// </summary>
[Serializable, NetSerializable]
public sealed class SkillsListEuiState : EuiStateBase
{
    public string TargetName;
    public List<SkillInfo> Skills;

    public SkillsListEuiState(string targetName, List<SkillInfo> skills)
    {
        TargetName = targetName;
        Skills = skills;
    }
}
