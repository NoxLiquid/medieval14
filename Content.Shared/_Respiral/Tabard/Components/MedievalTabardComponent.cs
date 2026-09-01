using Content.Shared._Respiral.Identity;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Respiral.Tabard.Components;

/// <summary>
/// Табард — накидка с гербом дома. Прикрепляется к одежде с <see cref="MedievalTabardAttachableComponent"/>;
/// его слой <c>equipped-TABARD</c> накладывается поверх экипированной одежды и её иконки.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MedievalTabardComponent : Component
{
    /// <summary>
    /// RSI, из которой берутся слои табарда при отрисовке поверх одежды.
    /// </summary>
    [DataField(required: true)]
    public ResPath Rsi;

    /// <summary>
    /// Состояние наложения на экипированную одежду (4 направления).
    /// </summary>
    [DataField]
    public string EquippedState = "equipped-TABARD";

    /// <summary>
    /// Состояние наложения на иконку предмета одежды.
    /// </summary>
    [DataField]
    public string IconState = "icon";

    /// <summary>
    /// Дом, к которому принадлежит табард. Задел под печати/сверку грамот.
    /// </summary>
    [DataField]
    public ProtoId<HouseSealPrototype>? House;

    /// <summary>
    /// Аффилиация дома (PresumedJob), которую даёт ношение брони с этим табардом.
    /// Показывается окружающим при скрытом лице носителя.
    /// </summary>
    [DataField]
    public LocId? Affiliation;

    /// <summary>
    /// Длительность прикрепления (DoAfter).
    /// </summary>
    [DataField]
    public TimeSpan AttachTime = TimeSpan.FromSeconds(2);
}
