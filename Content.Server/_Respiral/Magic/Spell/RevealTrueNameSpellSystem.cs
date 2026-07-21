using Content.Shared._Respiral.Identity;
using Content.Shared._Respiral.Magic.Spell;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Popups;

namespace Content.Server._Respiral.Magic.Spell;

/// <summary>
/// Форма «ритуал по цели»: обрабатывает <see cref="RevealTrueNameSpellEvent"/> — нацелено на человека,
/// списывает ману, запускает DoAfter и по его завершении раскрывает кастеру истинное имя цели
/// (пишет <c>TrueName</c> в его <see cref="KnownFacesComponent"/>). Это магический канал узнавания
/// истинного имени из <c>.claude/skills/identity.md</c>: обходит аноним и ложный паспорт.
/// Server-only (эффект серверный; кулдаун/звук действия предсказывает движок).
/// </summary>
public sealed partial class RevealTrueNameSpellSystem : EntitySystem
{
    [Dependency] private SpellSystem _spell = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private MedievalIdentitySystem _identity = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevealTrueNameSpellEvent>(OnRevealSpell);
        SubscribeLocalEvent<IdentityComponent, RevealTrueNameDoAfterEvent>(OnRevealDoAfter);
    }

    private void OnRevealSpell(RevealTrueNameSpellEvent args)
    {
        if (args.Handled)
            return;

        var performer = args.Performer;
        var target = args.Target;

        // Узнавать имя имеет смысл только у того, у кого есть личность (и оно ещё не своё).
        if (target == performer || !HasComp<IdentityComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("spell-reveal-truename-invalid"), performer, performer);
            return;
        }

        // Уже знаем истинное имя этой цели — не тратим ману впустую.
        if (_identity.KnowsTrueName(performer, target))
        {
            _popup.PopupEntity(Loc.GetString("spell-reveal-truename-already"), performer, performer);
            return;
        }

        // Списание маны — на старте каста (как у прочих форм); при срыве DoAfter мана уже потрачена.
        if (!_spell.TryCast(args.Action.Owner, performer))
            return;

        args.Handled = true;

        var doAfter = new DoAfterArgs(EntityManager, performer, args.Delay, new RevealTrueNameDoAfterEvent(), target, target: target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
        };
        _doAfter.TryStartDoAfter(doAfter);

        _popup.PopupEntity(Loc.GetString("spell-reveal-truename-begin"), target, performer);
    }

    private void OnRevealDoAfter(Entity<IdentityComponent> ent, ref RevealTrueNameDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var performer = args.User;

        var trueName = _identity.RevealTrueName(performer, ent.Owner);

        _popup.PopupEntity(Loc.GetString("spell-reveal-truename-success", ("name", trueName)), performer, performer);
        args.Handled = true;
    }
}
