using System.Linq;
using Content.Shared._Respiral.Identity.Components;
using Content.Shared._Respiral.Identity.Events;
using Content.Shared._Respiral.Skill;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Respiral.Identity;

/// <summary>
/// Средневековое распознавание: грамоты/паспорта
/// при скрытом лице, со сверкой описания владельца с предъявителем и проверкой печати дома,
/// плюс режим «предъявить всем».
/// </summary>
public sealed partial class MedievalIdentitySystem : EntitySystem
{
    [Dependency] private HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private IdentitySystem _identity = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    /// <summary>
    /// Имя с бумаги приводится к одной строке не длиннее этого
    /// </summary>
    private const int MaxNameLength = 64;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdentityComponent, ModifyPresumedIdentityEvent>(OnGetCredential);
        SubscribeLocalEvent<IdentityComponent, GetVerbsEvent<ExamineVerb>>(OnIdentityExamineVerb);
        SubscribeLocalEvent<KnownFacesComponent, ComponentGetStateAttemptEvent>(OnKnownFacesGetStateAttempt);
        SubscribeLocalEvent<KnownFacesComponent, ResolveIdentityNameEvent>(OnResolveName);
        SubscribeLocalEvent<KnownFacesComponent, CanSeeThroughIdentityEvent>(OnCanSeeThrough);
        SubscribeLocalEvent<MedievalCredentialComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<MedievalCredentialComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<MedievalCredentialComponent, GotUnequippedHandEvent>(OnUnequippedHand);
        SubscribeLocalEvent<MedievalCredentialComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<MedievalCredentialComponent, MedievalCredentialDoAfterEvent>(OnCredentialDoAfter);
    }

    //   паспорт не заверён → выбираем будущего владельца (цель заверения);
    //   паспорт заверён    → предъявляем его этому человеку («предъявить одному»).
    private void OnAfterInteract(Entity<MedievalCredentialComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        // Цель должна быть человеком: для незаверённого — чьё описание снимем печатью,
        // для заверённого — тем, у кого есть восприятие личности.
        if (ent.Comp.Seal == null)
        {
            if (!HasComp<HumanoidProfileComponent>(target))
                return;
        }
        else
        {
            // Предъявлять заверённый паспорт самому себе бессмысленно
            if (target == args.User)
            {
                _popup.PopupEntity(Loc.GetString("medieval-credential-present-one-self"), args.User, args.User);
                return;
            }

            if (!HasComp<IdentityComponent>(target))
                return;
        }

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay,
            new MedievalCredentialDoAfterEvent(), ent.Owner, target: target, used: ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        args.Handled = _doAfter.TryStartDoAfter(doAfter);
    }

    // «Предъявить одному»: если предъявитель сходится с описанием в паспорте (и печать валидна) —
    // цель запоминает предъявителя под именем ИЗ ПАСПОРТА (KnownFaces) и впредь зовёт его так.
    // Имя может быть ложным: показав другой паспорт с другим именем, можно перезаписать запомненное.
    // Глобальная идентичность при этом не меняется.
    private void OnCredentialDoAfter(Entity<MedievalCredentialComponent> ent, ref MedievalCredentialDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target is not { } target)
            return;

        var presenter = args.Args.User;

        // Незаверённый паспорт: фиксируем будущего владельца.
        if (ent.Comp.Seal == null)
        {
            ent.Comp.Target = target;
            Dirty(ent);
            _popup.PopupEntity(Loc.GetString("medieval-credential-target-selected"), target, presenter);
            args.Handled = true;
            return;
        }

        // Читатель должен уметь читать: неграмотный не разберёт имя на бумаге и ничего не запомнит.
        if (Literacy.GetLevel(target, EntityManager) < ent.Comp.ReadingLiteracy)
        {
            _popup.PopupEntity(Loc.GetString("medieval-credential-present-one-illiterate"), target, presenter);
            args.Handled = true;
            return;
        }

        if (ent.Comp.Name is not { } name
            || !IsSealValid(ent.Comp)
            || !MatchesDescription(presenter, ent.Comp.OwnerDescription))
        {
            _popup.PopupEntity(Loc.GetString("medieval-credential-present-one-fail"), target, presenter);
            args.Handled = true;
            return;
        }

        // Запоминаем предъявителя: дописываем имя из паспорта в историю заметки.
        // Текущим становится последнее показанное; прежние сохраняются в «ранее назывался».
        var face = EnsureKnownFace(target, presenter, out var known);

        // Не плодим дубли, если показали тот же паспорт повторно.
        if (face.ShownNames.Count == 0 || face.ShownNames[^1] != name)
            face.ShownNames.Add(name);

        Dirty(target, known);

        _popup.PopupEntity(Loc.GetString("medieval-credential-present-one-success"), target, presenter);
        args.Handled = true;
    }

    /// <summary>
    /// Заметка <paramref name="viewer"/>'а о <paramref name="target"/>, создаётся при первом обращении.
    /// Общий хелпер для всех источников знания об именах (паспорт, магия…).
    /// при вызове нужно пометить компонент, как грязный <c>Dirty(viewer, known)</c>.
    /// </summary>
    private KnownFace EnsureKnownFace(EntityUid viewer, EntityUid target, out KnownFacesComponent known)
    {
        known = EnsureComp<KnownFacesComponent>(viewer);
        var net = GetNetEntity(target);
        if (!known.Known.TryGetValue(net, out var face))
        {
            face = new KnownFace();
            known.Known[net] = face;
        }

        return face;
    }

    /// <summary>
    /// Раскрывает <paramref name="viewer"/>'у истинное имя <paramref name="target"/>
    /// </summary>
    public string RevealTrueName(EntityUid viewer, EntityUid target)
    {
        var face = EnsureKnownFace(viewer, target, out var known);
        var trueName = Name(target);
        face.TrueName = trueName;
        Dirty(viewer, known);
        return trueName;
    }

    /// <summary>Знает ли <paramref name="viewer"/> истинное имя <paramref name="target"/>.</summary>
    public bool KnowsTrueName(EntityUid viewer, EntityUid target)
    {
        return TryComp<KnownFacesComponent>(viewer, out var known)
            && known.Known.TryGetValue(GetNetEntity(target), out var face)
            && face.TrueName != null;
    }

    // Заменяем показанное имя: истинное важнее показанного по паспорту, иначе — последнее показанное
    private void OnResolveName(Entity<KnownFacesComponent> ent, ref ResolveIdentityNameEvent args)
    {
        if (!ent.Comp.Known.TryGetValue(GetNetEntity(args.Target), out var face))
            return;

        if (face.TrueName is { } trueName)
            args.Name = trueName;
        else if (face.ShownNames.Count > 0)
            args.Name = face.ShownNames[^1];
    }

    // Сквозь аноним видно только знающему истинное имя
    private void OnCanSeeThrough(Entity<KnownFacesComponent> ent, ref CanSeeThroughIdentityEvent args)
    {
        if (ent.Comp.Known.TryGetValue(GetNetEntity(args.Target), out var face) && face.TrueName != null)
            args.CanSee = true;
    }

    /// <summary>
    /// Личные заметки об именах — приватные: их состояние уходит только клиенту-владельцу
    /// (сервер всегда держит полную копию).
    /// </summary>
    private void OnKnownFacesGetStateAttempt(Entity<KnownFacesComponent> ent, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = args.Player?.AttachedEntity != ent.Owner;
    }

    /// <summary>
    /// Верб осмотра «Имена»: показывает зрителю его личную заметку об этом человеке
    /// (текущее имя, ранее показанные, истинное — если известно). По образцу осмотра брони.
    /// </summary>
    private void OnIdentityExamineVerb(Entity<IdentityComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        // Заметка хранится у зрителя; верб появляется, только если ему есть что вспомнить.
        if (!TryComp<KnownFacesComponent>(args.User, out var known)
            || !known.Known.TryGetValue(GetNetEntity(ent.Owner), out var face))
            return;

        _examine.AddDetailedExamineVerb(args, ent.Comp, BuildNotesMarkup(face),
            Loc.GetString("medieval-identity-notes-verb-text"),
            "/Textures/Interface/VerbIcons/human-head-eyes.svg.192dpi.png",
            Loc.GetString("medieval-identity-notes-verb-message"));
    }

    /// <summary>
    /// Собирает текст заметки об именах человека.
    /// </summary>
    private FormattedMessage BuildNotesMarkup(KnownFace face)
    {
        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(Loc.GetString("medieval-identity-notes-header"));

        if (face.ShownNames.Count > 0)
        {
            msg.PushNewline();
            msg.AddMarkupOrThrow(Loc.GetString("medieval-identity-notes-current",
                ("name", FormattedMessage.EscapeText(face.ShownNames[^1]))));
        }

        if (face.ShownNames.Count > 1)
        {
            // Прежние имена, без текущего, в порядке предъявления.
            var previous = string.Join(", ",
                face.ShownNames.GetRange(0, face.ShownNames.Count - 1).Select(FormattedMessage.EscapeText));
            msg.PushNewline();
            msg.AddMarkupOrThrow(Loc.GetString("medieval-identity-notes-previous", ("names", previous)));
        }

        if (face.TrueName is { } trueName)
        {
            msg.PushNewline();
            msg.AddMarkupOrThrow(Loc.GetString("medieval-identity-notes-true",
                ("name", FormattedMessage.EscapeText(trueName))));
        }

        return msg;
    }

    /// <summary>
    /// Работает только при скрытом лице — иначе движок и так показывает настоящее имя.
    /// </summary>
    private void OnGetCredential(Entity<IdentityComponent> ent, ref ModifyPresumedIdentityEvent args)
    {
        if (!TryComp<PresentingCredentialComponent>(ent, out var presenting))
            return;

        if (!TryComp<MedievalCredentialComponent>(presenting.Passport, out var cred))
            return;

        // Лишь пока носитель держит его в руке. Если его убрали/украли/
        if (!_hands.IsHolding(ent.Owner, presenting.Passport))
            return;

        // Аффилиация (ливрея) — без сверки описания, как униформа.
        if (cred.Affiliation is { } affiliation)
            args.Representation.PresumedJob = Loc.GetString(affiliation);

        // Личное имя — только при валидной печати и совпадении описания (жёсткий блок).
        if (cred.Name is not { } name)
            return;

        if (!IsSealValid(cred) || !MatchesDescription(ent.Owner, cred.OwnerDescription))
            return;

        args.Representation.PresumedName = name;
    }

    /// <summary>
    /// Грубая сверка описания владельца грамоты с носителем (пол + возрастная группа + вид).
    /// </summary>
    private bool MatchesDescription(EntityUid uid, CredentialDescription? description)
    {
        if (description is not { } desc)
            return false;

        if (!TryComp<HumanoidProfileComponent>(uid, out var profile))
            return false;

        if (profile.Gender != desc.Gender || profile.Species != desc.Species)
            return false;

        return GetAgeBracket(profile.Species, profile.Age) == desc.AgeBracket;
    }

    /// <summary>
    /// Возрастная группа сущности: пороги из <see cref="SpeciesPrototype"/>.
    /// </summary>
    private AgeBracket GetAgeBracket(ProtoId<SpeciesPrototype> species, int age)
    {
        if (!_proto.TryIndex(species, out var speciesProto) || age < speciesProto.YoungAge)
            return AgeBracket.Young;

        return age < speciesProto.OldAge ? AgeBracket.MiddleAged : AgeBracket.Old;
    }

    /// <summary>
    /// Локализованное название возрастной группы.
    /// </summary>
    private string GetAgeBracketRepresentation(AgeBracket bracket)
    {
        return bracket switch
        {
            AgeBracket.Young => Loc.GetString("identity-age-young"),
            AgeBracket.MiddleAged => Loc.GetString("identity-age-middle-aged"),
            AgeBracket.Old or _ => Loc.GetString("identity-age-old"),
        };
    }

    /// <summary>Печать валидна, если заверена существующим домом. 
    ///  TODO Подделка и отзыв.</summary>
    private bool IsSealValid(MedievalCredentialComponent cred)
    {
        return cred.Seal is { } seal && _proto.HasIndex(seal);
    }

    private void OnGetVerbs(Entity<MedievalCredentialComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;
        var passport = ent.Owner;
        var presenting = TryComp<PresentingCredentialComponent>(user, out var pc) && pc.Passport == passport;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString(presenting ? "medieval-credential-verb-conceal" : "medieval-credential-verb-present"),
            Act = () => SetPresenting(user, passport, !presenting),
            Priority = 1,
        });
    }

    private void SetPresenting(EntityUid user, EntityUid passport, bool present)
    {
        if (present)
        {
            if (TryComp<PresentingCredentialComponent>(user, out var old) && old.Passport != passport)
                _appearance.SetData(old.Passport, MedievalCredentialVisuals.Presented, false);

            var comp = EnsureComp<PresentingCredentialComponent>(user);
            comp.Passport = passport;
            Dirty(user, comp);
        }
        else
        {
            RemComp<PresentingCredentialComponent>(user);
        }

        _appearance.SetData(passport, MedievalCredentialVisuals.Presented, present);
        _identity.QueueIdentityUpdate(user);
    }

    private void OnUnequippedHand(Entity<MedievalCredentialComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (!TryComp<PresentingCredentialComponent>(args.User, out var pc) || pc.Passport != ent.Owner)
            return;

        RemComp<PresentingCredentialComponent>(args.User);
        _appearance.SetData(ent.Owner, MedievalCredentialVisuals.Presented, false);
        _identity.QueueIdentityUpdate(args.User);
    }

    /// <summary>
    /// Заверение печатью дома снимает описание с ранее выбранного будущего владельца
    /// (<see cref="MedievalCredentialComponent.Target"/>) и вписывает имя с бумаги.
    /// </summary>
    private void OnInteractUsing(Entity<MedievalCredentialComponent> ent, ref InteractUsingEvent args)
    {
        if (!TryComp<MedievalHouseSealComponent>(args.Used, out var seal))
            return;

        // Паспорт заверяется 1 раз.
        if (ent.Comp.Seal != null)
        {
            _popup.PopupEntity(Loc.GetString("medieval-credential-already-sealed"), args.User, args.User);
            args.Handled = true;
            return;
        }

        // Печать заверяет паспорт лишь на заранее выбранного владельца — иначе некого описывать.
        if (ent.Comp.Target is not { } owner || !TryComp<HumanoidProfileComponent>(owner, out var profile))
        {
            _popup.PopupEntity(Loc.GetString("medieval-credential-certify-no-target"), args.User, args.User);
            return;
        }

        // Пустой паспорт заверять нельзя: иначе печать есть, а имени нет — и цель уже не выбрать (тупик).
        if (!TryComp<PaperComponent>(ent.Owner, out var paper) || string.IsNullOrWhiteSpace(paper.Content))
        {
            _popup.PopupEntity(Loc.GetString("medieval-credential-empty"), args.User, args.User);
            return;
        }

        ent.Comp.Name = SanitizeName(paper.Content);

        // Снимок описания владельца — по нему потом сверяется предъявитель.
        var ageBracket = GetAgeBracket(profile.Species, profile.Age);
        ent.Comp.OwnerDescription = new CredentialDescription
        {
            Gender = profile.Gender,
            Species = profile.Species,
            AgeBracket = ageBracket,
        };

        // Заверение автоматически вписывает в бумагу приметы владельца:
        // пол, расу и возрастную группу (по ним потом и сверяется предъявитель).
        var description = Loc.GetString("medieval-credential-description",
            ("gender", GetGenderRepresentation(profile.Gender)),
            ("species", _humanoidProfile.GetSpeciesRepresentation(profile.Species).ToLower()),
            ("age", GetAgeBracketRepresentation(ageBracket)));

        _paper.SetContent((ent.Owner, paper), $"{paper.Content.TrimEnd()}\n\n{description}");

        // Заверённый паспорт больше не редактируется: имя зафиксировано и не должно
        // расходиться с механическим cred.Name.
        paper.EditingDisabled = true;
        Dirty(ent.Owner, paper);

        ent.Comp.Seal = seal.House;
        Dirty(ent);
        // args.Handled не ставим: PaperSystem ниже наносит косметический штамп.
    }

    /// <summary>
    /// Имя с бумаги: обрезаем до одной строки и по длине.
    /// </summary>
    private static string SanitizeName(string content)
    {
        var name = content.Trim();

        var newline = name.IndexOfAny(new[] { '\n', '\r' });
        if (newline >= 0)
            name = name[..newline].Trim();

        if (name.Length > MaxNameLength)
            name = name[..MaxNameLength].Trim();

        return name;
    }

    /// <summary>
    /// Локализованное название пола для примет паспорта
    /// </summary>
    private string GetGenderRepresentation(Gender gender)
    {
        return gender switch
        {
            Gender.Female => Loc.GetString("identity-gender-feminine"),
            Gender.Male => Loc.GetString("identity-gender-masculine"),
            Gender.Epicene or Gender.Neuter or _ => Loc.GetString("identity-gender-person"),
        };
    }
}
