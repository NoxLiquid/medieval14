using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server.Mind;
using Content.Server._Respiral.UI;
using Content.Shared._Respiral.Skill;
using Content.Shared._Respiral.Skill.Components;
using Content.Shared.Ghost;
using Content.Shared.Verbs;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._Respiral.Skill;

public sealed partial class SkillShareSystem : EntitySystem
{
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private EuiManager _euiManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private MindSystem _mindSystem = default!;
    [Dependency] private SkillSystem _skillSystem = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    [Dependency] private ConfirmDialogSystem _confirmDialog = default!;

    private const float MaxShareDistance = 3f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SkillComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetVerbs(Entity<SkillComponent> entity, ref GetVerbsEvent<Verb> args)
    {
        if (args.User == args.Target)
            return;

        var user = args.User;
        var target = entity.Owner;

        var isAdminGhost = false;
        if (HasComp<GhostComponent>(args.User) &&
            _playerManager.TryGetSessionByEntity(args.User, out var userSession) &&
            _adminManager.IsAdmin(userSession))
        {
            isAdminGhost = true;
        }

        // Для пользователей, не являющихся администраторами, применяются стандартные ограничения.
        if (!isAdminGhost)
        {
            if (HasComp<GhostComponent>(args.User))
                return;

            if (!_mindSystem.TryGetMind(args.User, out _, out _))
                return;

            if (!HasComp<SkillComponent>(args.User))
                return;

            if (!args.CanInteract || !args.CanAccess)
                return;

            var verb = new Verb
            {
                Text = Loc.GetString("skill-share-verb-text"),
                Category = VerbCategory.Interaction,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/information.svg.192dpi.png")),
                Act = () => RequestSkillShare(user, target),
                Priority = -1
            };

            args.Verbs.Add(verb);
        }
        else
        {
            var adminVerb = new Verb
            {
                Text = Loc.GetString("skill-share-verb-admin-text"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/information.svg.192dpi.png")),
                Act = () => OpenSkillsDirectly(user, target),
                Priority = -1
            };

            args.Verbs.Add(adminVerb);
        }
    }

    private void RequestSkillShare(EntityUid requester, EntityUid target)
    {
        var requesterXform = Transform(requester);
        var targetXform = Transform(target);

        if (!requesterXform.Coordinates.TryDistance(EntityManager, _transformSystem, targetXform.Coordinates, out var distance))
            return;

        if (distance > MaxShareDistance)
            return;

        if (!_playerManager.TryGetSessionByEntity(target, out var targetSession))
            return;

        var requesterName = MetaData(requester).EntityName;

        // Спрашиваем цель — показать ли навыки; при «Да» открываем список запросившему.
        _confirmDialog.OpenConfirmDialog(
            targetSession,
            Loc.GetString("skill-share-request-title"),
            Loc.GetString("skill-share-request-message", ("name", requesterName)),
            yesAction: () => ShowSkillsTo(target, requester));
    }

    private void OpenSkillsDirectly(EntityUid viewer, EntityUid target)
    {
        if (!_playerManager.TryGetSessionByEntity(viewer, out var viewerSession))
            return;

        if (!TryComp<SkillComponent>(target, out var skillComponent))
            return;

        // Админ-призрак видит точные уровни всех навыков.
        var skills = BuildSkillList(target, null, skillComponent);

        var targetName = MetaData(target).EntityName;

        _euiManager.OpenEui(new SkillsListEui(targetName, skills), viewerSession);
    }

    /// <summary>
    ///     Собирает список навыков цели для показа смотрящему. Для каждого навыка <see cref="SkillInfo.Evaluable"/>
    ///     истинно, только если смотрящий владеет навыком не хуже цели; иначе уровень скрывается.
    ///     <paramref name="viewer"/> null — оценивать может всё (админ-призрак).
    /// </summary>
    private List<SkillInfo> BuildSkillList(EntityUid target, EntityUid? viewer, SkillComponent targetComp)
    {
        var skills = new List<SkillInfo>();
        foreach (var (skillId, targetProgress) in targetComp.Skills)
        {
            var info = _skillSystem.GetSkillInfo(target, skillId, targetComp);
            if (info == null)
                continue;

            var value = info.Value;
            if (viewer != null)
                value.Evaluable = _skillSystem.GetSkillProgress(viewer.Value, skillId) >= targetProgress;

            skills.Add(value);
        }

        return skills;
    }

    /// <summary>
    ///     Открывает список навыков <paramref name="target"/> для <paramref name="requester"/>
    ///     после согласия цели. Дистанция перепроверяется — цель могла отойти за время диалога.
    /// </summary>
    private void ShowSkillsTo(EntityUid target, EntityUid requester)
    {
        if (!Exists(requester))
            return;

        var requesterXform = Transform(requester);
        var targetXform = Transform(target);

        if (!requesterXform.Coordinates.TryDistance(EntityManager, _transformSystem, targetXform.Coordinates, out var distance))
            return;

        if (distance > MaxShareDistance)
            return;

        if (!_playerManager.TryGetSessionByEntity(requester, out var requesterSession))
            return;

        if (!TryComp<SkillComponent>(target, out var skillComponent))
            return;

        // Запросивший видит уровень навыка, только если сам знает его не хуже цели.
        var skills = BuildSkillList(target, requester, skillComponent);

        var targetName = MetaData(target).EntityName;

        _euiManager.OpenEui(new SkillsListEui(targetName, skills), requesterSession);
    }
}
