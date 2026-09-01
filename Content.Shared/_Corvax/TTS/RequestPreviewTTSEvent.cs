using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Corvax.TTS;

[Serializable, NetSerializable]
public sealed class RequestPreviewTTSEvent(string voiceId) : EntityEventArgs
{
    public ProtoId<TTSVoicePrototype> VoiceId { get; } = voiceId;
}
