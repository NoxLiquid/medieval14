using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Respiral.Identity.Components;

/// <summary>
/// Личные заметки об именах: что этот зритель знает о других людях. Ключ — личность
/// (по <see cref="NetEntity"/>), значение — <see cref="KnownFace"/> с историей показанных
/// имён и (если известно) истинным именем.
/// Знакомый «видит сквозь» анонимную публичную идентичность и читает запомненное имя
/// (см. <c>Identity.Name</c> / <c>Identity.CanSeeThroughIdentity</c>).
/// Заполняется предъявлением паспорта «одному».
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class KnownFacesComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<NetEntity, KnownFace> Known = new();
}

/// <summary>
/// Заметка зрителя об одном человеке: имена, под которыми тот ему представлялся, и истинное имя.
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public sealed partial class KnownFace
{
    /// <summary>
    /// История показанных имён по порядку предъявления. Последнее — это текущее.
    /// </summary>
    [DataField]
    public List<string> ShownNames = new();

    /// <summary>
    /// Истинное имя, если оно стало известно. Если задано — отображается
    /// всегда, поверх показанного по паспорту.
    /// </summary>
    [DataField]
    public string? TrueName;
}
