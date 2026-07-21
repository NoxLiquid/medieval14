using Content.Shared._Respiral.Skill.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Respiral.Skill.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class SkillComponent : Component
{
    /// <summary>
    ///     Группа навыков
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<SkillGroupPrototype>? Group;

    /// <summary>
    ///     ID навыка и процент его изученности от 0 до 1
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<SkillPrototype>, float> Skills { get; set; } = new Dictionary<ProtoId<SkillPrototype>, float>();
}


