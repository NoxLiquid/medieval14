using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Friendly.Faction;

[RegisterComponent]
public sealed partial class FriendlyFactionComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<NpcFactionPrototype>> Factions = new();
}
