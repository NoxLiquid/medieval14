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
  (strips `+`), `RequestPreviewTTSEvent`. Whisper picks obfuscated vs clear audio per
  distance (`ChatSystem.VoiceRange` / `WhisperClearRange`). Max message 200 chars.
- `TTSSystem.SSML.cs` — wraps text in `<speak><prosody rate/pitch>`.
- `TTSSystem.Sanitize.cs` — cyrillic normalization, latin→cyr translit, abbreviation
  expansion, number→words.
- `TTSSystem.RateLimit.cs` — per-player limit via `PlayerRateLimitManager` (key `TTS`).
- `TTSVoiceAssignSystem.cs` — **Respiral-specific**, replaces the lobby voice picker:
  assigns a random `ttsVoice` matching the entity's `Sex` on `PlayerSpawnCompleteEvent`
  (players, uses `ev.Profile.Sex`) and `TTSComponent MapInit` (humanoid NPCs, uses
  `HumanoidProfileComponent.Sex`).

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
