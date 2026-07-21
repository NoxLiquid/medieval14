using Content.Server._Respiral.Skill.Components;
using Content.Server.Inventory;
using Content.Server.Popups;
using Content.Shared._Respiral.Skills.Events;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Server.Audio;

namespace Content.Server._Respiral.Skill;

public sealed partial class LearnSkillWhenUsingSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SkillSystem _skillSystem = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private AudioSystem _audio = default!;
    // [Dependency] private LanguageSystem _languageSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LearnSkillWhenUsingComponent, UseInHandEvent>(OnUseInHand, after: new[] { typeof(OpenableSystem), typeof(ServerInventorySystem) });
        SubscribeLocalEvent<LearnSkillWhenUsingComponent, LearnDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<LearnSkillWhenUsingComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, LearnSkillWhenUsingComponent component, ExaminedEvent args)
    {
        if (component.Skills.Count == 0)
            return;

        args.PushMarkup(Loc.GetString("skill-exm-learn-skills",
            ("skills", _skillSystem.GetSkillNames(component.Skills))));
    }

    private void OnUseInHand(EntityUid uid, LearnSkillWhenUsingComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        // if (component.LanguagesWhitelist != null &&
        //     component.LanguagesWhitelist.Count > 0)
        // {
        //     bool knowsAtLeastOne = false;
        //     foreach (var lang in component.LanguagesWhitelist)
        //     {
        //         if (_languageSystem.KnowsLanguage(args.User, lang))
        //         {
        //             knowsAtLeastOne = true;
        //             break;
        //         }
        //     }

        //     if (!knowsAtLeastOne)
        //     {   
        //         _popup.PopupEntity(Loc.GetString("skill-canlearn-language-missing"), args.User, args.User);
        //         return;
        //     }
        // }

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            TimeSpan.FromSeconds(component.Duration),
            new LearnDoAfterEvent(),
            eventTarget: uid,
            target: args.User,
            used: uid)
        {
            BreakOnHandChange = false,
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            DistanceThreshold = 1f,
            DuplicateCondition = DuplicateConditions.SameEvent,
            BlockDuplicate = true,
            CancelDuplicate = false
        };

        int unknown = 0;

        foreach (var skill in component.Skills)
        {
            if (_skillSystem.CanLearn(args.User, skill))
                unknown += 1;
        }

        if (unknown > 0)
        {
            if (!_doAfter.TryStartDoAfter(doAfterArgs))
                _popup.PopupEntity(Loc.GetString("skill-canlearn-already-learning"), args.User, args.User);
        }
        else
        {
            return;
        }

        if (component.Sound != null)
            _audio.PlayPvs(component.Sound, uid);
    }

    private void OnDoAfter(EntityUid uid, LearnSkillWhenUsingComponent component, LearnDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !Exists(args.Used))
            return;

        foreach (var skill in component.Skills)
        {
            _skillSystem.AddSkillProgress(args.User, skill, component.Points[skill]);
        }

        if (component.Sound != null)
            _audio.PlayPvs(component.Sound, uid);

        args.Repeat = true;
    }

}
