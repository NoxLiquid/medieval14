using Robust.Shared.Prototypes;
using Content.Shared._Respiral.Skill;
using Content.Shared._Respiral.Skill.Prototypes;
using Content.Shared._Respiral.Skill.Components;
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
    }

    private void OnPolymorphed(EntityUid uid, SkillComponent component, PolymorphedEvent args)
    {
        var skill = EnsureComp<SkillComponent>(args.NewEntity);
        skill.Skills = new Dictionary<ProtoId<SkillPrototype>, float>(component.Skills);
    }

    private void OnInit(Entity<SkillComponent> entity, ref ComponentInit args)
    {
        if (!_prototypeManager.TryIndex(entity.Comp.Group, out var group))
            return;

        foreach (var skill in group.Skills)
        {
            if (!entity.Comp.Skills.ContainsKey(skill))
                entity.Comp.Skills[skill] = 1f;
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

    public void AddSkillProgress(EntityUid uid, ProtoId<SkillPrototype> prototypeId, float progress, SkillComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (!_prototypeManager.TryIndex(prototypeId, out _))
        {
            _sawmill.Warning($"Прототип навыка {prototypeId} не найден");
            return;
        }

        if (component.Skills.TryGetValue(prototypeId, out var currentProgress))
            component.Skills[prototypeId] = Math.Min(1f, currentProgress + progress);
        else
            component.Skills[prototypeId] = Math.Min(1f, progress);

        Dirty(uid, component);
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
    ///     Проверяет, что у пользователя прогресс каждого навыка не ниже указанного порога (0..1).
    ///     Аналог <see cref="CheckRequiredSkills"/>, но с настраиваемым порогом вместо полного освоения.
    ///     Нет <see cref="SkillComponent"/> (например, NPC) — проверка пропускается. Сам показывает попап.
    /// </summary>
    public bool CheckRequiredSkillsProgress(EntityUid user, Dictionary<ProtoId<SkillPrototype>, float> neededSkills)
    {
        if (neededSkills.Count == 0)
            return true;

        if (!TryComp<SkillComponent>(user, out var skillComponent))
            return true;

        var missingSkills = new List<string>();

        foreach (var (skill, requiredProgress) in neededSkills)
        {
            if (!_prototypeManager.TryIndex(skill, out var skillPrototype) || skillPrototype == null)
            {
                _sawmill.Warning($"Прототип навыка {skill} не найден");
                continue;
            }

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
