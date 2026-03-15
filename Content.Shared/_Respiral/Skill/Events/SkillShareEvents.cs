using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Respiral.Skills.Events;

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

/// <summary>
/// EUI state for asking player if they want to share their skills.
/// </summary>
[Serializable, NetSerializable]
public sealed class SkillShareRequestEuiState : EuiStateBase
{
    public string RequesterName;

    public SkillShareRequestEuiState(string requesterName)
    {
        RequesterName = requesterName;
    }
}

/// <summary>
/// Message sent when player responds to skill share request.
/// </summary>
[Serializable, NetSerializable]
public sealed class SkillShareResponseMessage : EuiMessageBase
{
    public bool Accepted;

    public SkillShareResponseMessage(bool accepted)
    {
        Accepted = accepted;
    }
}
