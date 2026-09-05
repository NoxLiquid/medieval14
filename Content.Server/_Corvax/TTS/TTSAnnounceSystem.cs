using Content.Server.Communications;
using Content.Server.Station.Systems;
using Content.Shared._Corvax.TTS;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Corvax.TTS;

// Respiral: озвучка анонсов консоли связи голосом отправителя.
public sealed partial class TTSAnnounceSystem : EntitySystem
{
    [Dependency] private TTSSystem _tts = default!;
    [Dependency] private StationSystem _station = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CommunicationConsoleAnnouncementEvent>(OnConsoleAnnouncement);
    }

    private void OnConsoleAnnouncement(ref CommunicationConsoleAnnouncementEvent ev)
    {
        // нет голоса у отправителя — анонс не озвучиваем
        if (ResolveSenderVoice(ev.Sender) is not { } voiceId)
            return;

        // глобальный анонс — всем, станционный — по владеющей станции консоли
        var filter = ev.Component.Global
            ? Filter.Broadcast()
            : _station.GetInOwningStation(ev.Uid);

        if (filter.Count == 0)
            return;

        _tts.PlayTTS(ev.Text, voiceId, filter);
    }

    private ProtoId<TTSVoicePrototype>? ResolveSenderVoice(EntityUid? sender)
    {
        if (sender is not { } uid ||
            !TryComp<TTSComponent>(uid, out var tts))
            return null;

        var voiceId = tts.VoicePrototypeId?.Id;
        if (string.IsNullOrEmpty(voiceId))
            return null;

        // учитываем маску голоса
        var voiceEv = new TransformSpeakerVoiceEvent(uid, voiceId);
        RaiseLocalEvent(uid, voiceEv);
        return voiceEv.VoiceId;
    }
}
