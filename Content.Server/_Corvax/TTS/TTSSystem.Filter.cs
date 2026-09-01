using System.Threading.Tasks;
using Content.Shared._Corvax.TTS;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Corvax.TTS;

// Respiral: воспроизведение TTS на произвольный Filter (анонсы, радио, группа).
public sealed partial class TTSSystem
{
    /// <summary>
    ///     Озвучивает текст голосом-прототипом и проигрывает всем в фильтре.
    ///     sourceUid задан — звук позиционный от сущности, null — глобальный.
    /// </summary>
    public async void PlayTTS(
        string text,
        ProtoId<TTSVoicePrototype> voiceId,
        Filter filter,
        NetEntity? sourceUid = null,
        bool isWhisper = false)
    {
        if (!_isEnabled)
            return;

        if (!_prototypeManager.TryIndex(voiceId, out var protoVoice))
            return;

        await PlayTTS(text, protoVoice.Speaker, filter, sourceUid, isWhisper, protoVoice.Pitch, protoVoice.Rate);
    }

    /// <summary>
    ///     Низкоуровневый вариант: принимает id спикера Silero и prosody напрямую.
    /// </summary>
    public async Task PlayTTS(
        string text,
        string speaker,
        Filter filter,
        NetEntity? sourceUid = null,
        bool isWhisper = false,
        string pitch = "",
        string rate = "")
    {
        if (!_isEnabled)
            return;

        var soundData = await GenerateTTS(text, speaker, pitch, rate, isWhisper);
        if (soundData is null)
            return;

        RaiseNetworkEvent(new PlayTTSEvent(soundData, sourceUid, isWhisper), filter);
    }
}
