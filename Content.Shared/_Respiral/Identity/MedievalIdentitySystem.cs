using Content.Shared._Respiral.Identity.Components;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Respiral.Identity;

/// <summary>
/// Средневековое распознавание: грамоты/паспорта питают «предполагаемое» имя
/// при скрытом лице, со сверкой описания владельца с предъявителем и проверкой печати дома,
/// плюс режим «предъявить всем».
/// </summary>
public sealed partial class MedievalIdentitySystem : EntitySystem
{
    [Dependency] private HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private IdentitySystem _identity = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private ExamineSystemShared _examine = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdentityComponent, GetMedievalCredentialEvent>(OnGetCredential);
        SubscribeLocalEvent<IdentityComponent, GetVerbsEvent<ExamineVerb>>(OnIdentityExamineVerb);
        SubscribeLocalEvent<KnownFacesComponent, ComponentGetStateAttemptEvent>(OnKnownFacesGetStateAttempt);
        SubscribeLocalEvent<MedievalCredentialComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<MedievalCredentialComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<MedievalCredentialComponent, DroppedEvent>(OnDropped);
        SubscribeLocalEvent<MedievalCredentialComponent, AfterInteractEvent>(OnPresentToOne);
    }

    // «Предъявить одному»: клик своим паспортом по человеку.
    // Если предъявитель сходится с описанием в паспорте (и печать валидна) — цель запоминает
    // предъявителя под именем ИЗ ПАСПОРТА (KnownFaces) и впредь зовёт его так. Имя может быть
    // ложным: показав другой паспорт с другим именем, можно перезаписать запомненное.
    // Глобальная идентичность при этом не меняется.
    private void OnPresentToOne(Entity<MedievalCredentialComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } viewer)
            return;

        // Узнать может лишь тот, у кого вообще есть восприятие личности.
        if (!HasComp<IdentityComponent>(viewer))
            return;

        var presenter = args.User;
        if (ent.Comp.Name is not { } name
            || !IsSealValid(ent.Comp)
            || !MatchesDescription(presenter, ent.Comp.OwnerDescription))
        {
            _popup.PopupClient(Loc.GetString("medieval-credential-present-one-fail"), viewer, presenter);
            args.Handled = true;
            return;
        }

        // Запоминаем предъявителя: дописываем имя из паспорта в историю заметки.
        // Текущим становится последнее показанное; прежние сохраняются в «ранее назывался».
        var face = EnsureKnownFace(viewer, presenter, out var known);

        // Не плодим дубли, если показали тот же паспорт повторно.
        if (face.ShownNames.Count == 0 || face.ShownNames[^1] != name)
            face.ShownNames.Add(name);

        Dirty(viewer, known);

        _popup.PopupClient(Loc.GetString("medieval-credential-present-one-success"), viewer, presenter);
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

    /// <summary>Собирает текст заметки об именах человека.</summary>
    private FormattedMessage BuildNotesMarkup(KnownFace face)
    {
        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(Loc.GetString("medieval-identity-notes-header"));

        if (face.ShownNames.Count > 0)
        {
            msg.PushNewline();
            msg.AddMarkupOrThrow(Loc.GetString("medieval-identity-notes-current",
                ("name", face.ShownNames[^1])));
        }

        if (face.ShownNames.Count > 1)
        {
            // Прежние имена, без текущего, в порядке предъявления.
            var previous = string.Join(", ", face.ShownNames.GetRange(0, face.ShownNames.Count - 1));
            msg.PushNewline();
            msg.AddMarkupOrThrow(Loc.GetString("medieval-identity-notes-previous", ("names", previous)));
        }

        if (face.TrueName is { } trueName)
        {
            msg.PushNewline();
            msg.AddMarkupOrThrow(Loc.GetString("medieval-identity-notes-true", ("name", trueName)));
        }

        return msg;
    }

    /// <summary>
    /// Работает только при скрытом лице — иначе движок и так показывает настоящее имя.
    /// </summary>
    private void OnGetCredential(Entity<IdentityComponent> ent, ref GetMedievalCredentialEvent args)
    {
        if (!TryComp<PresentingCredentialComponent>(ent, out var presenting))
            return;

        if (!TryComp<MedievalCredentialComponent>(presenting.Passport, out var cred))
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

    /// <summary>Грубая сверка описания владельца грамоты с носителем (пол + возрастная группа + вид).</summary>
    private bool MatchesDescription(EntityUid uid, CredentialDescription? description)
    {
        if (description is not { } desc)
            return false;

        if (!TryComp<HumanoidProfileComponent>(uid, out var profile))
            return false;

        if (profile.Gender != desc.Gender || profile.Species != desc.Species)
            return false;

        return _humanoidProfile.GetAgeRepresentation(profile.Species, profile.Age) == desc.AgeBracket;
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
            var comp = EnsureComp<PresentingCredentialComponent>(user);
            comp.Passport = passport;
            Dirty(user, comp);
        }
        else
        {
            RemComp<PresentingCredentialComponent>(user);
        }

        // Разворачивание грамоты меняет её спрайт (см. MedievalCredentialVisualizerSystem).
        _appearance.SetData(passport, MedievalCredentialVisuals.Presented, present);
        _identity.QueueIdentityUpdate(user);
    }

    /// <summary>Развёрнутая грамота, выпавшая из рук, перестаёт предъявляться.</summary>
    private void OnDropped(Entity<MedievalCredentialComponent> ent, ref DroppedEvent args)
    {
        if (!TryComp<PresentingCredentialComponent>(args.User, out var pc) || pc.Passport != ent.Owner)
            return;

        RemComp<PresentingCredentialComponent>(args.User);
        _appearance.SetData(ent.Owner, MedievalCredentialVisuals.Presented, false);
        _identity.QueueIdentityUpdate(args.User);
    }

    /// <summary>
    /// Заверение печатью дома фиксирует снимок описания держателя
    /// </summary>
    private void OnInteractUsing(Entity<MedievalCredentialComponent> ent, ref InteractUsingEvent args)
    {
        if (!TryComp<MedievalHouseSealComponent>(args.Used, out var seal))
            return;

        // Снимок описания держателя грамоты (будущий владелец держит её при заверении).
        if (GetHolder(ent.Owner) is { } owner && TryComp<HumanoidProfileComponent>(owner, out var profile))
        {
            ent.Comp.OwnerDescription = new CredentialDescription
            {
                Gender = profile.Gender,
                Species = profile.Species,
                AgeBracket = _humanoidProfile.GetAgeRepresentation(profile.Species, profile.Age),
            };
        }

        // Имя — из написанного на бумаге.
        if (TryComp<PaperComponent>(ent.Owner, out var paper) && !string.IsNullOrWhiteSpace(paper.Content))
            ent.Comp.Name = paper.Content.Trim();

        ent.Comp.Seal = seal.House;
        Dirty(ent);
        // args.Handled PaperSystem ниже наносит косметический штамп.
    }

    private EntityUid? GetHolder(EntityUid item)
    {
        return _container.TryGetContainingContainer((item, null, null), out var container)
            ? container.Owner
            : null;
    }
}
