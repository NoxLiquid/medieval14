using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Respiral.Identity;

/// <summary>
/// Снимок внешнего описания владельца грамоты. При предъявлении сверяется с предъявителем:
/// Описание грубое — в движке нет «примет/шрамов», только пол + возрастная группа + вид,
/// поэтому краденая грамота сработает на похожем по этим трём осям.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public partial struct CredentialDescription
{
    [DataField]
    public Gender Gender;

    /// <summary>
    /// Возрастная группа (результат <c>HumanoidProfileSystem.GetAgeRepresentation</c>),
    /// а не точный возраст — сравнение идёт по группе.
    /// </summary>
    [DataField]
    public string AgeBracket;

    [DataField]
    public ProtoId<SpeciesPrototype> Species;
}
