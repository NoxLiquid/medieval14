# TTS (Text-To-Speech)

Voice synthesis of in-game speech (say/whisper). Ported from the Corvax TTS module,
adapted to the Respiral fork. Placed in the `_Corvax` namespace/dirs (per owner decision,
overriding the usual `_Respiral/` convention) so it stays easy to diff against the
upstream Corvax reference.

## Two halves

1. **External synth service** — lives OUTSIDE this repo at `D:\TTS` (Dockerized FastAPI +
   Silero `v4_ru`, CPU-only). Speaks the Corvax HTTP/JSON protocol. Not part of the game
   build. See that repo's `README.md`.
2. **Game integration** — this document. Sends chat text to the service over HTTP and plays
   the returned OGG in-world.

## HTTP contract (game ↔ service)

`POST {tts.api_url}` JSON `{api_token, text (SSML), speaker, ssml, put_accent, put_yo,
sample_rate, format}` → `{results:[{audio: <base64 ogg>}], original_sha1}`. HTTP `429` is
treated as rate-limited (audio dropped). Defined in `TTSManager.cs` (request/response
structs). This is fixed by the external service — do not change field names/casing.

## File map

Shared (`Content.Shared/_Corvax/`):
- `CCCVars/CCCVars.cs` — cvars: `tts.enabled`, `tts.api_url`, `tts.api_token`,
  `tts.api_timeout`, `tts.volume` (client), `tts.max_cache`, `tts.rate_limit_period`,
  `tts.rate_limit_count`.
- `TTS/TTSComponent.cs` — `voice` datafield (`ProtoId<TTSVoicePrototype>`, nullable → auto-assigned).
- `TTS/TTSVoicePrototype.cs` — `[Prototype("ttsVoice")]`: `name` (loc id), `sex`, `speaker`
  (Silero speaker id), `roundStart`.
- `TTS/PlayTTSEvent.cs`, `RequestPreviewTTSEvent.cs`, `TransformSpeakerVoiceEvent.cs`.

Server (`Content.Server/_Corvax/TTS/`):
- `TTSManager.cs` — IoC manager, HTTP client + in-memory LRU cache. Registered in
  `ServerContentIoC` and `.Initialize()`d in `EntryPoint`.
- `TTSSystem.cs` — subscribes `EntitySpokeEvent` (say/whisper), `TransformSpeechEvent`
  (strips `+`), `RequestPreviewTTSEvent`. Max message 200 chars. `HandleSay`/`HandleWhisper`
  now route through the `TTSSystem.Filter.cs` `PlayTTS(...)` API instead of building
  `PlayTTSEvent` themselves: say → `PlayTTS(msg, speaker, Filter.Pvs(uid), sourceUid)`;
  whisper partitions PVS recipients by distance into a clear filter (≤`WhisperClearRange`)
  and a muffled filter (≤`VoiceRange`) and calls `PlayTTS` per non-empty group (skips synth
  when a group is empty).
- `TTSSystem.SSML.cs` — wraps text in `<speak><prosody rate/pitch>`.
- `TTSSystem.Sanitize.cs` — cyrillic normalization, latin→cyr translit, abbreviation
  expansion, number→words.
- `TTSSystem.RateLimit.cs` — per-player limit via `PlayerRateLimitManager` (key `TTS`).
- `TTSSystem.Filter.cs` — **Respiral-specific**, reusable public API for playing TTS to an
  arbitrary `Filter` (announcements, radio, any group broadcast), independent of the
  per-entity say/whisper path. Two overloads:
  `PlayTTS(string text, ProtoId<TTSVoicePrototype> voiceId, Filter filter, NetEntity? sourceUid = null, bool isWhisper = false)`
  and a lower-level one taking a raw Silero `speaker` string. Generates audio via the shared
  `GenerateTTS`, then `RaiseNetworkEvent(new PlayTTSEvent(soundData, sourceUid, isWhisper), filter)`.
  `sourceUid` set → client plays positional (anchored to the entity); null → global
  (`_audio.PlayGlobal`). No rate limit / no `MaxMessageChars` cap (server-initiated, caller's
  responsibility). No-op when `tts.enabled` is false. Callers: `HandleSay`/`HandleWhisper`
  in `TTSSystem.cs` now use it; also available for future announcement/radio voicing.
  Ported *idea* from the DeadSpace fork
  (`dead-space-14 Content.Server/Corvax/TTS/TTSSystem.cs` `HandleAnnounce/HandleRadio`) but
  written fresh: DeadSpace's version is coupled to a `LanguageSystem`/lexicon + `RadioSpokeEvent`/
  `AnnounceSpokeEvent`/`EntitySpokeToEntityEvent`, **none of which exist in this fork**.
- `TTSAnnounceSystem.cs` — **Respiral-specific**, voices communications-console announcements.
  Subscribes the upstream `CommunicationConsoleAnnouncementEvent` (ByRefEvent, raised in
  `Content.Server/Communications/CommunicationsConsoleSystem.cs` with the sanitized text
  *before* the "sent by" suffix) and calls `TTSSystem.PlayTTS(text, voiceId, filter)`.
  Voice = the **sender's** voice (from their `TTSComponent`, run through
  `TransformSpeakerVoiceEvent` for voice masks); if the sender has no voice, the announcement
  is **not** voiced (no fallback). Audience: `comp.Global` → `Filter.Broadcast()`, else
  `StationSystem.GetInOwningStation(consoleUid)`; skips synth when the filter is empty. No
  upstream file edits — subscribes to an event upstream already raises. TTS plays immediately
  (overlaps the announcement chime; DeadSpace delays its announce TTS ~6s — not done here).
- `TTSVoiceAssignSystem.cs` — **Respiral-specific**, replaces the lobby voice picker:
  assigns a random `ttsVoice` matching the entity's `Sex` on `PlayerSpawnCompleteEvent`
  (players, uses `ev.Profile.Sex`) and `TTSComponent MapInit` (humanoid NPCs, uses
  `HumanoidProfileComponent.Sex`).

Admin announcement voice picker (**Respiral**, upstream files edited inline):
- `Content.Client/Administration/UI/AdminAnnounceWindow.xaml{,.cs}` — adds a `VoiceSearch`
  `LineEdit` + `VoiceList` `ItemList` under the announcement text: lists every
  `TTSVoicePrototype` by localized `Name` (sorted, case-insensitive substring search),
  single-select. A **"No voice"** item (`admin-announce-voice-none`, metadata null) is always
  first and unfiltered; selecting it sets `SelectedVoice = null` (no TTS) and it's the default.
  `SelectedVoice` (prototype id, nullable) survives filtering; re-selected visually on
  repopulate via `Item.Selected` (item-level event only, no list-event recursion). XAML wraps
  the added controls in `<!-- Respiral-TTS-Start/End -->` markers.
- `Content.Client/Administration/UI/AdminMenuWindowEui.cs` — sends `Voice = _window.SelectedVoice`.
- `Content.Shared/Administration/AdminAnnounceEuiState.cs` — `DoAnnounce.Voice` (`string?`).
- `Content.Server/Administration/UI/AdminAnnounceEui.cs` — resolves `TTSSystem`, and after
  dispatch calls `PlayTTS(text, new ProtoId<TTSVoicePrototype>(voice), Filter.Broadcast())`
  when a voice is picked. **Must** wrap the id in `ProtoId` explicitly — otherwise the
  `(string text, string speaker, …)` overload wins and the id is misread as a Silero speaker.
  No voice picked → announcement text only, no TTS. Loc keys `admin-announce-voice-*` in
  `Locale/{en-US,ru-RU}/administration/ui/admin-announce-window.ftl`.

Client (`Content.Client/_Corvax/TTS/TTSSystem.cs`): receives `PlayTTSEvent`, writes OGG
to an in-memory `MemoryContentRoot` under `/TTS`, plays via `AudioSystem`
(entity-positional or global for preview). Volume from `tts.volume`.

Resources:
- `Prototypes/_Corvax/TTS/voices.yml` — 5 Silero `v4_ru` voices: `Aidar`/`Eugene` (Male),
  `Kseniya`/`Baya`/`Xenia` (Female). Add more only if the service's model exposes them.
- `Locale/{en-US,ru-RU}/_corvax/tts.ftl` — `tts-rate-limited` + voice names.

## Upstream hooks (marked `// Respiral-Start Corvax-TTS` … `// Respiral-End`)

- `Resources/Prototypes/Body/species_base.yml` — `- type: TTS` on `BaseSpeciesMobOrganic`
  (all organic humanoids; `MobHuman` inherits it).
- `Content.Server/IoC/ServerContentIoC.cs` — `deps.Register<TTSManager>()`.
- `Content.Server/Entry/EntryPoint.cs` — `[Dependency] TTSManager` + `_ttsManager.Initialize()`.

Add these to [[upstream_patches.md]] when auditing merges.

## Fork adaptations (differences from the Corvax reference)

- `HumanoidAppearanceComponent` is renamed to `HumanoidProfileComponent` in this fork
  (component `HumanoidProfile`, holds `Sex`).
- `EntitySpokeEvent` / `TransformSpeechEvent` live in `Content.Shared.Chat`
  (`SharedChatEvents.cs`), not the server chat namespace.
- Chat ranges (`VoiceRange=10`, `WhisperClearRange=2`, `WhisperMuffledRange=5`) are consts
  on `SharedChatSystem`.
- Profile field `Voice` here is upstream `EmoteSoundsPrototype` (emote sounds), NOT a TTS
  voice — do not reuse it.

## Enabling

Server config (`server_config.toml` or cvars): `tts.enabled = true`,
`tts.api_url = http://127.0.0.1:5001/tts`, `tts.api_token = <shared secret>`.

## Deployment (home server) — verified 2026-08-31

Service installed **from the public repo** `https://github.com/NoxLiquid/TTS.git`:
`git clone` into `/home/ivan/TTS`, then `docker compose up -d --build` (docker needs
`sudo`; ivan not in docker group). Container `respiral-tts`, `restart: unless-stopped`,
bound to `127.0.0.1:${TTS_HOST_PORT}`.

- **Port 5000 is taken by `SS14.Watchdog`** on this host → TTS host port moved to **5001**
  via server-local `/home/ivan/TTS/.env` (`TTS_HOST_PORT=5001`). Compose port is
  `127.0.0.1:${TTS_HOST_PORT:-5000}:5000`.
- Verified: `/health` = ok, `/tts` returns a valid `OggS` stream.
- Dockerfile pins `PIP_DEFAULT_TIMEOUT=120` + `PIP_RETRIES=10` (pypi.org was flaky from
  the server during build).
- The live game server does NOT yet run the game-side TTS build — enabling in-game
  requires publishing the `ss14-respiral` changes (commit → GitHub Actions Publish → CDN →
  watchdog update) and setting the `tts.*` cvars in the instance `config.toml`.

## Live server status — verified 2026-09-01

The full pipeline is live end-to-end on the home server:

- **Build deployed**: Publish → CDN → watchdog Manifest update pulled the TTS build.
  Instance `bin/` (updated 2026-09-01 08:54) contains `Resources/Prototypes/_Corvax/TTS/
  voices.yml` and the TTS types in `Content.Server.dll` / `Content.Shared.dll`. Game server
  runs engine build `282.0.0.0`.
- **TTS enabled in game config**: appended `[tts]` to
  `/opt/ss14-watchdog/instances/main/config.toml` (owned by `ss14`; edit needs `sudo` as
  `ivan`). A `.bak.<ts>` backup is left beside it. Values:
  ```toml
  [tts]
  enabled = true
  api_url = "http://127.0.0.1:5001/tts"
  api_token = "respiral-tts"
  ```
- **Service is tokenless**: `/home/ivan/TTS/.env` sets only `TTS_HOST_PORT=5001` (no
  `TTS_API_TOKEN`), so the service does NOT validate the token — the `api_token` above is a
  placeholder and any value works. Service binds `127.0.0.1:5001` only. To harden later, set
  `TTS_API_TOKEN` in `.env`, `docker compose up -d` (recreate), and match `api_token` in
  `config.toml`.
- **Restart**: game server restarted via watchdog REST API
  `POST http://127.0.0.1:5000/instances/main/restart`, **HTTP Basic** auth
  username=`main` (instance key) / password=`<instance ApiToken>`. Came back clean, round
  incremented, no `unknown cvar` warning for `tts.*` (cvars recognized), no startup errors.
- **Not verifiable server-side**: actual in-game voice playback needs a connected player;
  the whole server path (valid config, accepted cvars, reachable service) is confirmed.
- **Access**: SSH to home is via VPS `85.198.69.124` (root/pw) as jump host →
  `ivan@10.8.0.2` (WireGuard) with key `HOST/secrets/id_nox_admin`. `ivan` is in `sudo` but
  NOT in the `docker` group (docker needs `sudo`).

## Known limitations / deferred (Phase 2, NOT implemented)

- No in-lobby voice selection UI. Voices are auto-assigned by sex only. Adding a picker
  requires a NEW TTS-voice field on `HumanoidCharacterProfile` (serialization + networking
  + DB migration on schema `ss14`) + a character-editor tab + (optionally) sponsor gating
  interfaces the fork lacks. Reference: `space-station-14`
  `Content.Client/Corvax/{TTS/TTSTab, Lobby/UI/HumanoidProfileEditor.TTS}`.
- Voice mask does not change TTS voice: `TransformSpeakerVoiceEvent` is raised but no
  `VoiceMaskComponent.VoiceId` subscriber exists (fork's `VoiceMaskComponent` has no
  `VoiceId`). Hook is ready for a future port.
- `RequestPreviewTTSEvent` handler exists server-side but nothing triggers it client-side
  (no lobby UI yet).

## Build status

Server + client compile clean (validated). Prototype/locale loading not yet run through an
integration test.
