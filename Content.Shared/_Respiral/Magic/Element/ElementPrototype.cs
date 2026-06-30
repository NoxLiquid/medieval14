using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Shared._Respiral.Magic.Element;

/// <summary>
/// Природный элемент (огонь, вода, земля…). First-class сущность: на него ссылаются
/// заклинания и снаряды через <see cref="ProtoId{T}"/>. Задел под взаимодействия элементов
/// (будущая data-driven матрица реакций).
/// </summary>
[Prototype]
public sealed partial class ElementPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Локализованное имя элемента.
    /// </summary>
    [DataField]
    public LocId Name = string.Empty;

    /// <summary>
    /// Урон, который наносит эффект этого элемента (переносится на снаряд при касте).
    /// </summary>
    [DataField]
    public DamageSpecifier Damage = new();

    // TODO: правила взаимодействия элементов (ElementReactionPrototype) — на будущее.
}
