using Content.Shared._Respiral.Tabard;
using Content.Shared._Respiral.Tabard.Components;
using Content.Shared.Containers;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Server._Respiral.Tabard;

/// <summary>
/// Прикрепление/снятие табарда к одежде с флагом <see cref="MedievalTabardAttachableComponent"/>.
/// Взаимодействие табардом по одежде запускает DoAfter; снятие — через глагол (контекстное меню).
/// Отрисовка накладки — на клиенте (см. MedievalTabardVisualsSystem), по сетевому кэшу флаг-компонента.
/// </summary>
public sealed partial class MedievalTabardSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IdentitySystem _identity = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedievalTabardComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<MedievalTabardComponent, MedievalTabardDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<MedievalTabardAttachableComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<MedievalTabardAttachableComponent, ComponentInit>(OnAttachableInit);
        SubscribeLocalEvent<MedievalTabardAttachableComponent, MapInitEvent>(OnAttachableMapInit,
            after: [typeof(ContainerFillSystem)]);
    }

    // Контейнер под табард создаём заранее (на init), чтобы ContainerFill мог вложить табард на спавне.
    private void OnAttachableInit(Entity<MedievalTabardAttachableComponent> ent, ref ComponentInit args)
    {
        _container.EnsureContainer<ContainerSlot>(ent.Owner, MedievalTabardAttachableComponent.ContainerId);
    }

    // Табард, вложенный на спавне (через ContainerFill), — снимаем с него кэш отрисовки/аффилиации.
    private void OnAttachableMapInit(Entity<MedievalTabardAttachableComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.RsiPath != null)
            return;

        if (!_container.TryGetContainer(ent.Owner, MedievalTabardAttachableComponent.ContainerId, out var container)
            || container is not ContainerSlot { ContainedEntity: { } tabard }
            || !TryComp<MedievalTabardComponent>(tabard, out var tabardComp))
            return;

        SetTabardVisuals(ent, tabardComp);
        RefreshWearer(ent.Owner);
    }

    // Общий перенос характеристик табарда в сетевой кэш флаг-компонента.
    private void SetTabardVisuals(Entity<MedievalTabardAttachableComponent> ent, MedievalTabardComponent tabard)
    {
        ent.Comp.RsiPath = tabard.Rsi.ToString();
        ent.Comp.EquippedState = tabard.EquippedState;
        ent.Comp.IconState = tabard.IconState;
        ent.Comp.Affiliation = tabard.Affiliation?.Id;
        Dirty(ent);
    }

    private void OnAfterInteract(Entity<MedievalTabardComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!TryComp<MedievalTabardAttachableComponent>(target, out var attachable))
            return;

        args.Handled = true;

        // Уже есть табард — не даём прикрепить второй.
        if (attachable.RsiPath != null)
        {
            _popup.PopupEntity(Loc.GetString("medieval-tabard-occupied"), target, args.User);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.AttachTime,
            new MedievalTabardDoAfterEvent(), ent.Owner, target: target, used: ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnDoAfter(Entity<MedievalTabardComponent> ent, ref MedievalTabardDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        args.Handled = true;

        if (!TryComp<MedievalTabardAttachableComponent>(target, out var attachable) || attachable.RsiPath != null)
            return;

        var slot = _container.EnsureContainer<ContainerSlot>(target, MedievalTabardAttachableComponent.ContainerId);
        if (!_container.Insert(ent.Owner, slot))
            return;

        SetTabardVisuals((target, attachable), ent.Comp);
        RefreshWearer(target);

        _popup.PopupEntity(Loc.GetString("medieval-tabard-attached"), target, args.User);
    }

    private void OnGetVerbs(Entity<MedievalTabardAttachableComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || ent.Comp.RsiPath == null)
            return;

        var user = args.User;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("medieval-tabard-detach-verb"),
            Category = VerbCategory.Interaction,
            DoContactInteraction = true,
            Act = () => Detach(ent, user),
        });
    }

    private void Detach(Entity<MedievalTabardAttachableComponent> ent, EntityUid user)
    {
        if (!_container.TryGetContainer(ent.Owner, MedievalTabardAttachableComponent.ContainerId, out var container)
            || container is not ContainerSlot { ContainedEntity: { } tabard })
            return;

        _container.Remove(tabard, container);
        _hands.PickupOrDrop(user, tabard);

        ent.Comp.RsiPath = null;
        ent.Comp.EquippedState = null;
        ent.Comp.IconState = null;
        ent.Comp.Affiliation = null;
        Dirty(ent);
        RefreshWearer(ent.Owner);

        _popup.PopupEntity(Loc.GetString("medieval-tabard-detached"), ent.Owner, user);
    }

    // Если одежда надета — пересобрать идентичность носителя, чтобы аффилиация обновилась сразу.
    private void RefreshWearer(EntityUid clothing)
    {
        var wearer = Transform(clothing).ParentUid;
        if (HasComp<IdentityComponent>(wearer))
            _identity.QueueIdentityUpdate(wearer);
    }
}
