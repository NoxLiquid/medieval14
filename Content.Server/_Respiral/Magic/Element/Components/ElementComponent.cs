using Content.Shared._Respiral.Magic.Element;
using Robust.Shared.Prototypes;

namespace Content.Server._Respiral.Magic.Element.Components;

/// <summary>
/// Несёт природный элемент сущности (снаряда; позже — затронутой цели/тайла).
/// Server-only, НЕ networked: используется только серверной логикой (тип урона, будущие реакции).
/// </summary>
[RegisterComponent]
public sealed partial class ElementComponent : Component
{
    [DataField]
    public ProtoId<ElementPrototype>? Element;
}
