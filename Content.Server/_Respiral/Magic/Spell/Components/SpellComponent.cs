namespace Content.Server._Respiral.Magic.Spell.Components;

/// <summary>
/// Кросс-секущие данные заклинания на сущности-действии: стоимость маны и природный элемент.
/// Звук каста задаётся на ActionComponent.Sound (предсказан движком, бесплатно).
/// Server-only: читает только сервер; в YAML-прототипе действия клиент компонент игнорирует.
/// </summary>
[RegisterComponent]
public sealed partial class SpellComponent : Component
{
    /// <summary>
    /// Сколько маны стоит каст. 0 — бесплатно.
    /// </summary>
    [DataField]
    public float ManaCost;
}
