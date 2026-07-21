using Content.Client.CharacterInfo;
using Content.Shared._Respiral.Skills;
using Content.Shared._Respiral.Skills.Components;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._Respiral.Skill;

public sealed partial class SkillSystem : EntitySystem
{
    [Dependency] private CharacterInfoSystem _characterInfo = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    private SkillsListWindow? _skillsWindow;
    private List<SkillInfo> _currentSkills = new();
    private string? _currentTargetName;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CharacterInfoSystem.GetCharacterInfoControlsEvent>(OnGetCharacterInfoControls);
    }

    private void OnGetCharacterInfoControls(ref CharacterInfoSystem.GetCharacterInfoControlsEvent ev)
    {
        _currentSkills = GetSkills(ev.Entity);
        _currentTargetName = Name(ev.Entity);

        var button = new Button
        {
            Text = Loc.GetString("skill-character-button")
        };

        button.OnPressed += OnSkillsButtonPressed;
        ev.Controls.Add(button);
    }

    private void OnSkillsButtonPressed(BaseButton.ButtonEventArgs args)
    {
        _skillsWindow?.Close();
        _skillsWindow = new SkillsListWindow();
        _skillsWindow.SetSkills(_currentSkills, _currentTargetName);
        _skillsWindow.OpenCentered();
    }

    private List<SkillInfo> GetSkills(EntityUid entity)
    {
        var skills = new List<SkillInfo>();

        if (!TryComp<SkillComponent>(entity, out var skillComponent))
            return skills;

        foreach (var skill in skillComponent.Skills)
        {
            if (!_prototype.TryIndex(skill.Key, out var skillPrototype) || skillPrototype == null)
                continue;

            skills.Add(new SkillInfo(
                skillPrototype.Name,
                skillPrototype.Description,
                skillPrototype.Icon,
                skill.Value,
                skillPrototype.IconSize));
        }

        return skills;
    }
}