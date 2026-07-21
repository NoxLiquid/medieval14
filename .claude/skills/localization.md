# Localization (FTL)

## Where it lives

- `Resources/Locale/<culture>/...` — FTL files per culture (`en-US`, `ru-RU`).
- The subdirectory structure in `ru-RU` mirrors `en-US`.

## Main rule: parity with en-US

On an upstream merge, a message's text in `en-US` can change its **argument scheme**, not
just the words. The `ru-RU` translation must repeat the **form of arguments/functions** of the
English message, otherwise FTL throws `Unknown variable: $...` at runtime, and the
integration tests fail (the server pool reports it as `[ERRO] loc: "ru-RU"/...`).

The test checks the runtime resolution of strings, not the YAML itself — `dotnet build` doesn't
catch this. Verify by hand.

### Checks

Message-ID parity between cultures:

```sh
diff \
  <(grep -oE '^[a-z0-9-]+ =' Resources/Locale/en-US/<path>.ftl | sort) \
  <(grep -oE '^[a-z0-9-]+ =' Resources/Locale/ru-RU/<path>.ftl | sort)
```

Should be empty. If ru is missing IDs (new upstream messages) — add and
translate them; extra IDs in ru — remove.

Check for absence of stale variables after an edit:

```sh
grep -oE '\$[a-z]+' Resources/Locale/ru-RU/<path>.ftl | sort -u
```

The set of `$` variables must match en-US (allowing only for static text).

## Case: ion-storm (AI laws)

`Resources/Locale/{en-US,ru-RU}/station-events/events/ion-storm.ftl`.

Upstream (Wizard's Den) rewrote the ion-storm law system. Previously each
message received many separate variables (`$who`, `$joined`, `$subjects`,
`$adjective`, `$thing`, ...). Now each law is passed **a single argument
`$ion`**, and the dynamic parts are resolved via FTL term functions:
`ION-WHO($ion)`, `ION-NUMBER-BASE($ion)`, `ION-NUMBER-MOD($ion)`,
`ION-ADJECTIVE($ion)`, `ION-SUBJECT($ion)`, `ION-THING($ion)`, `ION-MUST($ion)`,
`ION-PLURAL($ion)`, `ION-PART($ion)`, etc.

The functions are registered for `DefaultCulture` in
`Content.Server/Silicons/Laws/IonLawLocalizationSystem.cs` (values taken from
`IonLawSystem.GetOrGenerateValue`). The functions ignore the `$ion` argument (`_ =>`),
it's only a marker.

The `ru-RU` translation was left on the old variable scheme → `Unknown variable`.
Fix: rewrite `ru-RU/.../ion-storm.ftl`, mirroring the `en-US` structure —
keep the `{ION-X($ion)}` calls and references to the term `{ion-storm-part}`, translate
only the static text and the branches of select expressions (`{ION-PLURAL($ion) -> [true] ТРЕБУЮТ *[false] ТРЕБУЕТ}`).
The helper terms (`ion-storm-you`, `ion-storm-the-job`, `ion-storm-adjective-things`,
`ion-storm-x-and-y`) keep their original arguments — don't touch them.

New messages that were absent in ru and were added: `ion-storm-law-who-dagd`
and the `ion-law-error-*` block (fallback strings in case law generation fails).
