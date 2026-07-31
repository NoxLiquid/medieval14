using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Respiral.Identity.Components;

/// <summary>
/// Средневековый документ. Несёт имя, аффилиацию дома и снимок описания владельца.
/// Имя присваивается идентичности носителя, только если описание владельца совпало с
/// предъявителем (иначе имя не выдаётся вовсе) и печать дома валидна. Аффилиация
/// выдаётся без сверки описания — это «форма», носить может кто угодно.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MedievalCredentialComponent : Component
{
    /// <summary>
    /// Личное имя, которое грамота присваивает (PresumedName).
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? Name;

    /// <summary>
    /// Аффилиация/титул дома (PresumedJob), сверки описания не требует (как униформа).
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? Affiliation;

    /// <summary>
    /// Снимок описания владельца для сверки с предъявителем. null = грамота не заверена как именная.
    /// </summary>
    [DataField, AutoNetworkedField]
    public CredentialDescription? OwnerDescription;

    /// <summary>
    /// Печать дома, заверившего грамоту. null = не заверена (имя силы не имеет).
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<HouseSealPrototype>? Seal;

    /// <summary>
    /// Выбранный будущий владелец. Пока паспорт не заверён, применение его на человека
    /// (через DoAfter) фиксирует сюда цель; при постановке печати с неё снимается описание.
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? Target;

    /// <summary>
    /// Длительность применения паспорта на человека (выбор цели заверения либо предъявление одному).
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Порог грамотности (0..1), нужный получателю, чтобы разобрать имя при предъявлении паспорта
    /// </summary>
    [DataField]
    public float ReadingLiteracy = 0.5f;
}
