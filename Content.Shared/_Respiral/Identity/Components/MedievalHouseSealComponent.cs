using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Respiral.Identity.Components;

/// <summary>
/// Печать дома. При заверении паспорта (использование на грамоте) проставляет ему
/// <see cref="HouseSealPrototype"/> и фиксирует снимок описания держателя.
/// Печать создаёт апстримный <c>PaperSystem</c> через сопутствующий <c>StampComponent</c>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MedievalHouseSealComponent : Component
{
    [DataField(required: true)]
    public ProtoId<HouseSealPrototype> House;
}
