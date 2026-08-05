using System.Collections.Generic;
using Content.Shared.Materials;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client._Respiral.Materials;

/// <summary>
/// Красит слои спрайта и in-hand визуал цветами материалов. Цвет задаётся один раз
/// в прототипе материала (поле <c>color</c>), а не хардкодится в каждой сущности.
/// Клиентский компонент (суффикс Visuals — сервер его игнорирует при загрузке прототипов).
/// </summary>
[RegisterComponent]
public sealed partial class MaterialColorVisualsComponent : Component
{
    /// <summary>Ключ слоя в LayerMap спрайта → материал, чьим цветом красится слой.</summary>
    [DataField]
    public Dictionary<string, ProtoId<MaterialPrototype>> Layers = new();

    /// <summary>Материал, чьим цветом красятся все слои in-hand визуала. null — не трогать.</summary>
    [DataField]
    public ProtoId<MaterialPrototype>? Inhand;
}
