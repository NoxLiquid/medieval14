using Robust.Shared.Prototypes;
using Content.Shared._Respiral.Skills;
using Content.Shared._Respiral.Skills.Prototypes;
using Content.Shared._Respiral.Skills.Components;
using Content.Server.Popups;
using System.Linq;
using Content.Shared.Polymorph;
using Content.Shared.Cloning.Events;

namespace Content.Server._Respiral.Skill;

public sealed partial class SkillSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private PopupSystem _popup = default!;
    private ISawmill _sawmill = default!;
    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("SkillSystem");

        SubscribeLocalEvent<SkillComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SkillComponent, PolymorphedEvent>(OnPolymorphed);
        SubscribeLocalEvent<SkillComponent, CloningEvent>(OnCloning);
    }

    private void OnCloning(Entity<SkillComponent> ent, ref CloningEvent args)
    {
        var skill = EnsureComp<SkillComponent>(args.CloneUid);
        skill.Skills = new Dictionary<ProtoId<SkillPrototype>, float>(ent.Comp.Skills);
        RefreshSkillGrants(args.CloneUid, skill);
    }

    private void OnPolymorphed(EntityUid uid, SkillComponent component, PolymorphedEvent args)
    {
        var skill = EnsureComp<SkillComponent>(args.NewEntity);
        skill.Skills = new Dictionary<ProtoId<SkillPrototype>, float>(component.Skills);
        RefreshSkillGrants(args.NewEntity, skill);
    }

    private void OnInit(Entity<SkillComponent> entity, ref ComponentInit args)
    {
        if (_prototypeManager.TryIndex(entity.Comp.Group, out var group))
        {
            foreach (var skill in group.Skills)
            {
                if (!entity.Comp.Skills.ContainsKey(skill))
                    entity.Comp.Skills[skill] = 1f;
            }
        }

        RefreshSkillGrants(entity.Owner, entity.Comp);
    }

    /// <summary>
    ///     Пересчитывает все выдаваемые навыками компоненты (<see cref="SkillPrototype.GrantComponent"/>):
    ///     навык на уровне порога и выше — компоненты выдаются, иначе снимаются.
    /// </summary>
    public void RefreshSkillGrants(EntityUid uid, SkillComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        foreach (var skillId in component.Skills.Keys)
        {
            if (_prototypeManager.TryIndex(skillId, out var prototype))
                UpdateSkillGrant(uid, prototype, component);
        }
    }

    /// <summary>
    ///     Выдаёт или снимает <see cref="SkillPrototype.GrantComponent"/> одного навыка в зависимости
    ///     от того, достигнут ли его порог <see cref="SkillPrototype.GrantThreshold"/>.
    /// </summary>
    private void UpdateSkillGrant(EntityUid uid, SkillPrototype prototype, SkillComponent component)
    {
        if (prototype.GrantComponents == null)
            return;

        var progress = GetSkillProgress(uid, prototype.ID, component);

        foreach (var (threshold, registry) in prototype.GrantComponents)
        {
            if (progress >= threshold)
                EntityManager.AddComponents(uid, registry, removeExisting: false);
            else
                EntityManager.RemoveComponents(uid, registry);
        }
    }

    public SkillInfo? GetSkillInfo(EntityUid uid, string prototypeId, SkillComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return null;

        if (!_prototypeManager.TryIndex<SkillPrototype>(prototypeId, out var skillPrototype) || skillPrototype == null)
        {
            _sawmill.Warning($"Прототип навыка {prototypeId} не найден");
            return null;
        }

        if (!component.Skills.TryGetValue(prototypeId, out var progress))
        {
            _sawmill.Warning($"Не удалось получить прогресс изучения навыка");
            return null;
        }

        SkillInfo skill = new SkillInfo(
            skillPrototype.Name,
            skillPrototype.Description,
            skillPrototype.Icon,
            progress,
            skillPrototype.IconSize
        );

        return skill;
    }

    public bool CnowThisSkill(EntityUid uid, ProtoId<SkillPrototype> prototypeId, SkillComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        return component.Skills.TryGetValue(prototypeId, out var progress) && progress >= 1f;
    }

    public float GetSkillProgress(EntityUid uid, ProtoId<SkillPrototype> prototypeId, SkillComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return 0f;

        if (!component.Skills.TryGetValue(prototypeId, out var progress))
            return 0f;

        return progress;
    }

    public bool CanLearn(EntityUid uid, ProtoId<SkillPrototype> prototypeId, SkillComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        if (!_prototypeManager.TryIndex(prototypeId, out var prototype))
        {
            _sawmill.Warning($"Прототип навыка {prototypeId} не найден");
            return false;
        }

        if (prototype.RequiredSkills != null
            && prototype.RequiredSkills.Count > 0
            && !CheckRequiredSkills(uid, prototype.RequiredSkills)
            )
            return false;

        return !CnowThisSkill(uid, prototypeId, component);
    }

    /// <summary>
    ///     Добавляет прогресс навыка, не превышая <paramref name="maxProgress"/> (0..1, по умолчанию — полное освоение).
    ///     Используется, например, чтобы ограничить потолок изучения конкретным источником обучения (книгой).
    /// </summary>
    public void AddSkillProgress(EntityUid uid, ProtoId<SkillPrototype> prototypeId, float progress, float maxProgress = 1f, SkillComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (!_prototypeManager.TryIndex(prototypeId, out var prototype))
        {
            _sawmill.Warning($"Прототип навыка {prototypeId} не найден");
            return;
        }

        if (component.Skills.TryGetValue(prototypeId, out var currentProgress))
            component.Skills[prototypeId] = Math.Min(maxProgress, currentProgress + progress);
        else
            component.Skills[prototypeId] = Math.Min(maxProgress, progress);

        Dirty(uid, component);

        UpdateSkillGrant(uid, prototype, component);
    }

    /// <summary>
    ///     Собирает названия навыков в одну строку через запятую (для описаний при осмотре).
    ///     Пропускает прототипы, которых нет.
    /// </summary>
    public string GetSkillNames(List<ProtoId<SkillPrototype>> skills)
    {
        var names = new List<string>();

        foreach (var skill in skills)
        {
            if (_prototypeManager.TryIndex(skill, out var skillPrototype) && skillPrototype != null)
                names.Add(skillPrototype.Name);
        }

        return string.Join(", ", names);
    }

    public bool CheckRequiredSkills(EntityUid user, List<ProtoId<SkillPrototype>> neededSkills)
    {
        if (!TryComp<SkillComponent>(user, out var skillComponent))
            return true;

        var missingSkills = new List<string>();

        foreach (var skill in neededSkills)
        {
            if (!_prototypeManager.TryIndex(skill, out var skillPrototype) || skillPrototype == null)
            {
                _sawmill.Warning($"Прототип навыка {skill} не найден");
                continue;
            }

            if (!CnowThisSkill(user, skill, skillComponent))
                missingSkills.Add(skillPrototype.Name);
        }

        if (missingSkills.Count > 0)
        {
            var skillsText = string.Join(", ", missingSkills.Select(s => Loc.GetString($"{s}")));
            var message = Loc.GetString("skill-need-skill-message", ("skills", skillsText));

            _popup.PopupEntity(message, user, user);
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Проверяет, что у пользователя уровень знания каждого навыка не ниже указанного.
    ///     Аналог <see cref="CheckRequiredSkills"/>, но с настраиваемым порогом вместо полного освоения.
    ///     Нет <see cref="SkillComponent"/> (например, NPC) — проверка пропускается. Сам показывает попап.
    /// </summary>
    public bool CheckRequiredSkillsProgress(EntityUid user, Dictionary<ProtoId<SkillPrototype>, SkillKnowledgeLevel> neededSkills)
    {
        if (neededSkills.Count == 0)
            return true;

        if (!TryComp<SkillComponent>(user, out var skillComponent))
            return true;

        var missingSkills = new List<string>();

        foreach (var (skill, requiredLevel) in neededSkills)
        {
            if (!_prototypeManager.TryIndex(skill, out var skillPrototype) || skillPrototype == null)
            {
                _sawmill.Warning($"Прототип навыка {skill} не найден");
                continue;
            }

            var requiredProgress = SkillKnowledge.GetMinProgress(requiredLevel);
            if (GetSkillProgress(user, skill, skillComponent) < requiredProgress)
                missingSkills.Add(skillPrototype.Name);
        }

        if (missingSkills.Count > 0)
        {
            var skillsText = string.Join(", ", missingSkills.Select(s => Loc.GetString($"{s}")));
            var message = Loc.GetString("skill-need-skill-message", ("skills", skillsText));

            _popup.PopupEntity(message, user, user);
            return false;
        }

        return true;
    }

}
