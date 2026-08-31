using Content.Shared._Corvax.TTS;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Corvax.TTS;

public sealed partial class TTSVoiceAssignSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private readonly Dictionary<Sex, List<string>> _voicesBySex = new();
    private readonly List<string> _allVoices = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<TTSComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
        _proto.PrototypesReloaded += OnPrototypesReloaded;
        CacheVoices();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _proto.PrototypesReloaded -= OnPrototypesReloaded;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        CacheVoices();
    }

    private void CacheVoices()
    {
        _voicesBySex.Clear();
        _allVoices.Clear();

        foreach (var voice in _proto.EnumeratePrototypes<TTSVoicePrototype>())
        {
            if (!voice.RoundStart)
                continue;

            if (!_voicesBySex.TryGetValue(voice.Sex, out var list))
            {
                list = new List<string>();
                _voicesBySex[voice.Sex] = list;
            }

            list.Add(voice.ID);
            _allVoices.Add(voice.ID);
        }
    }

    private void OnMapInit(EntityUid uid, TTSComponent component, MapInitEvent args)
    {
        if (!string.IsNullOrEmpty(component.VoicePrototypeId.Id))
            return;

        if (!TryComp<HumanoidProfileComponent>(uid, out var humanoid))
            return;

        AssignVoice(component, humanoid.Sex);
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent ev)
    {
        if (!TryComp<TTSComponent>(ev.Mob, out var component))
            return;

        AssignVoice(component, ev.Profile.Sex);
    }

    private void AssignVoice(TTSComponent component, Sex sex)
    {
        List<string>? voices = null;
        if (sex != Sex.Unsexed)
            _voicesBySex.TryGetValue(sex, out voices);

        if (voices == null || voices.Count == 0)
            voices = _allVoices;

        if (voices.Count == 0)
            return;

        component.VoicePrototypeId = _random.Pick(voices);
    }
}
