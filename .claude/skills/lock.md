# Lock / Key system (`_Respiral/Lock/`)

Medieval key/lock, distinct from the standard SS14 lock. Server systems:
`Content.Server/_Respiral/Lock/` — `MedievalKeySystem`, `MedievalKeyDuplicatorSystem`, `MedievalLockSystem`.
Shared components under `Content.Shared/_Respiral/Lock/`.

## Key components
- `MedievalKeyComponent` (`Content.Server/_Respiral/Lock/Components/`) — `KeyId` (string), `UseDuration` (3s),
  `IsLockpick` (bool), `UseSound` (`SoundSpecifier?`), and `Sound` (`EntityUid?`) — the **handle of the
  currently-playing use sound**, so it can be stopped.
- `MedievalLockComponent` (shared) — `AttachSound` etc.; a lock may be the target itself or resolved via
  `SharedMedievalLockSystem.GetLock(target)` (locks embedded in doors/containers).

## Use flow (`MedievalKeySystem`)
`AfterInteractEvent` on the key → resolve a `MedievalLockComponent` on/behind the target → start a
`UseKeyDoAfterEvent` DoAfter (`BreakOnMove`, `BreakOnDamage`, `DistanceThreshold 2f`). On the DoAfter event:
cancelled/no-target → stop sound; otherwise lock/unlock (`IsLockpick` toggles unconditionally, else
`TryLock`/`TryUnlock` by `KeyId`, popup `medieval-door-fail` on mismatch). `MedievalKeyDuplicatorSystem`
mirrors the same play/stop pattern for `DuplicateKeyDoAfterEvent`.

## Sound-on-cancel bug (fixed 2026-08)
The use-sound is played via `_audio.PlayPvs(UseSound, coords)?.Entity` and stored in `component.Sound`;
`OnDoAfter` stops it via `_audio.Stop(component.Sound)` on both cancel and completion. `_audio.Stop`
(`SharedAudioSystem.Stop`) resolves the audio entity and `QueueDel`s it (works on the server, where
`IsFirstTimePredicted` is always true) — so the stored-handle approach is correct.

**Bug:** the sound was played **before** `TryStartDoAfter` and **unconditionally**. If `TryStartDoAfter`
failed (e.g. a duplicate DoAfter from a second click, since it dedupes active DoAfters), a second sound
overwrote `component.Sound`, orphaning the first — the single active DoAfter's cancel then only stopped the
*last* handle, leaving the earlier sound playing forever.

**Fix:** call `TryStartDoAfter` first and `return` if it fails; stop any leftover `component.Sound`; then
play the sound **only** when the DoAfter actually started. Now every played sound has a DoAfter guaranteed to
stop it. Applied to both `MedievalKeySystem` and `MedievalKeyDuplicatorSystem`.

General rule: any DoAfter that plays a looping/long sound must (a) play it only after the DoAfter starts and
(b) store the audio `EntityUid?` and stop it in the DoAfter handler for **both** the cancelled and completed
branches. Note DoAfter cancellation *does* raise the event (`InternalCancel` → `RaiseDoAfterEvents` with
`Cancelled = true`), so the handler is the right place to clean up.
