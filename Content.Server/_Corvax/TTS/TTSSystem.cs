using System.Threading.Tasks;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared._Corvax.CCCVars;
using Content.Shared._Corvax.TTS;
using Content.Shared.GameTicking;
using Content.Shared.Players.RateLimiting;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Corvax.TTS;

public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private TTSManager _ttsManager = default!;
    [Dependency] private SharedTransformSystem _xforms = default!;
    [Dependency] private IRobustRandom _rng = default!;

    private readonly List<string> _sampleText =
        new()
        {
            "Съешь же ещё этих мягких французских булок, да выпей чаю.",
            "Клоун, прекрати разбрасывать банановые кожурки офицерам под ноги!",
            "Капитан, вы уверены что хотите назначить клоуна на должность главы персонала?",
            "Эс Бэ! Тут человек в сером костюме, с тулбоксом и в маске! Помогите!!",
            "Учёные, тут странная аномалия в баре! Она уже съела мима!",
            "Я надеюсь что инженеры внимательно следят за сингулярностью...",
            "Вы слышали эти странные крики в техах? Мне кажется туда ходить небезопасно.",
            "Вы не видели Гамлета? Мне кажется он забегал к вам на кухню.",
            "Здесь есть доктор? Человек умирает от отравленного пончика! Нужна помощь!",
            "Вам нужно согласие и печать квартирмейстера, если вы хотите сделать заказ на партию дробовиков.",
            "Возле эвакуационного шаттла разгерметизация! Инженеры, нам срочно нужна ваша помощь!",
            "Бармен, налей мне самого крепкого вина, которое есть в твоих запасах!"
        };

    private const int MaxMessageChars = 100 * 2;
    private bool _isEnabled = false;

    public override void Initialize()
    {
        _cfg.OnValueChanged(CCCVars.TTSEnabled, v => _isEnabled = v, true);

        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech);
        SubscribeLocalEvent<TTSComponent, EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeNetworkEvent<RequestPreviewTTSEvent>(OnRequestPreviewTTS);

        RegisterRateLimits();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _ttsManager.ResetCache();
    }

    private async void OnRequestPreviewTTS(RequestPreviewTTSEvent ev, EntitySessionEventArgs args)
    {
        if (!_isEnabled ||
            !_prototypeManager.TryIndex(ev.VoiceId, out var protoVoice))
            return;

        if (HandleRateLimit(args.SenderSession) != RateLimitStatus.Allowed)
            return;

        var previewText = _rng.Pick(_sampleText);
        var soundData = await GenerateTTS(previewText, protoVoice.Speaker, protoVoice.Pitch, protoVoice.Rate);
        if (soundData is null)
            return;

        RaiseNetworkEvent(new PlayTTSEvent(soundData), Filter.SinglePlayer(args.SenderSession));
    }

    private void OnEntitySpoke(EntityUid uid, TTSComponent component, EntitySpokeEvent args)
    {
        var voiceId = component.VoicePrototypeId;
        if (!_isEnabled ||
            args.Message.Length > MaxMessageChars ||
            string.IsNullOrEmpty(voiceId))
            return;

        var voiceEv = new TransformSpeakerVoiceEvent(uid, voiceId);
        RaiseLocalEvent(uid, voiceEv);
        voiceId = voiceEv.VoiceId;

        if (!_prototypeManager.TryIndex(voiceId, out var protoVoice))
            return;

        if (args.ObfuscatedMessage != null)
        {
            HandleWhisper(uid, args.Message, args.ObfuscatedMessage, protoVoice);
            return;
        }

        HandleSay(uid, args.Message, protoVoice);
    }

    private async void HandleSay(EntityUid uid, string message, TTSVoicePrototype voice)
    {
        // обычная речь: всем в PVS, позиционно от говорящего
        await PlayTTS(message, voice.Speaker, Filter.Pvs(uid), GetNetEntity(uid), pitch: voice.Pitch, rate: voice.Rate);
    }

    private async void HandleWhisper(EntityUid uid, string message, string obfMessage, TTSVoicePrototype voice)
    {
        // делим слушателей по дистанции: близкие слышат чётко, дальние приглушённо
        var source = GetNetEntity(uid);
        var clear = Filter.Empty();
        var muffled = Filter.Empty();

        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourcePos = _xforms.GetWorldPosition(xformQuery.GetComponent(uid), xformQuery);
        foreach (var session in Filter.Pvs(uid).Recipients)
        {
            if (!session.AttachedEntity.HasValue) continue;
            var xform = xformQuery.GetComponent(session.AttachedEntity.Value);
            var distance = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).Length();
            if (distance > ChatSystem.VoiceRange)
                continue;

            if (distance > ChatSystem.WhisperClearRange)
                muffled.AddPlayer(session);
            else
                clear.AddPlayer(session);
        }

        // если есть кому слушать
        if (clear.Count > 0)
            await PlayTTS(message, voice.Speaker, clear, source, isWhisper: true, pitch: voice.Pitch, rate: voice.Rate);

        if (muffled.Count > 0)
            await PlayTTS(obfMessage, voice.Speaker, muffled, source, isWhisper: true, pitch: voice.Pitch, rate: voice.Rate);
    }

    private async Task<byte[]?> GenerateTTS(string text, string speaker, string pitch = "", string rate = "", bool isWhisper = false)
    {
        var textSanitized = Sanitize(text);
        if (textSanitized == "") return null;
        if (char.IsLetter(textSanitized[^1]))
            textSanitized += ".";

        var textSsml = ToSsmlText(textSanitized, pitch, rate, isWhisper);

        return await _ttsManager.ConvertTextToSpeech(speaker, textSsml);
    }
}
