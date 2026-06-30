using Content.Server._Respiral.Magic.Mana;
using Content.Server._Respiral.Magic.Spell.Components;
using Content.Shared.Popups;

namespace Content.Server._Respiral.Magic.Spell;

/// <summary>
/// Ядро заклинаний: общие для всех «форм» проверки каста (мана, в будущем — прочие требования).
/// Конкретные формы (снаряд, луч…) — отдельные системы, дёргают <see cref="TryCast"/>.
/// </summary>
public sealed partial class SpellSystem : EntitySystem
{
    [Dependency] private ManaSystem _mana = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    /// <summary>
    /// Проверяет требования заклинания и списывает ману. Возвращает true, если каст разрешён.
    /// </summary>
    /// <remarks>
    /// Кулдаун/звук действия уже применены движком (предсказанно) до этого вызова — при нехватке
    /// маны они не откатываются. Полноценный pre-cast гейт требует реплицированной маны (отложено).
    /// </remarks>
    public bool TryCast(Entity<SpellComponent?> spell, EntityUid performer)
    {
        // Действие без SpellComponent — это не наше заклинание, не вмешиваемся.
        if (!Resolve(spell, ref spell.Comp, false))
            return true;

        // TODO: прочие требования каста (через событие) — на будущее.

        if (spell.Comp.ManaCost > 0f && !_mana.TryUseMana(performer, spell.Comp.ManaCost))
        {
            _popup.PopupEntity(Loc.GetString("spell-not-enough-mana"), performer, performer);
            return false;
        }

        return true;
    }
}
