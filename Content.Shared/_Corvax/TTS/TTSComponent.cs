using Robust.Shared.Prototypes;

namespace Content.Shared._Corvax.TTS;

[RegisterComponent]
public sealed partial class TTSComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("voice")]
    public ProtoId<TTSVoicePrototype> VoicePrototypeId { get; set; }
}
