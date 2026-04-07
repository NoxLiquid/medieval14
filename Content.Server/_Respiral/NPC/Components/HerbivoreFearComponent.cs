using Content.Shared._Respiral.TimeWindow;

namespace Content._Respiral.NPC.Components;

[RegisterComponent]
public sealed partial class HerbivoreFearComponent : Component
{
    [DataField]
    public TimedWindow WindowDuration = MedievalNPCDefaultsSettings.FearWindowDuration.Clone();

    [DataField]
    public float FleeDistance = 8f;
}