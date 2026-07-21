# Engine Submodule & Upstream Merge Policy

Covers the `RobustToolbox` engine submodule, the `no-submodule-update` CI
guard, and how to merge upstream content without breaking either.

## Remotes

| Remote | URL | Role |
|---|---|---|
| `origin` | `github.com/NoxLiquid/respiral` | Fork; PR base (`origin/master`) |
| `upstream` | `github.com/respiral-tree/ss14-respiral` | "Zeniks" upstream to merge from |
| `upstream-wizards` | `github.com/space-wizards/space-station-14` | Vanilla SS14 |

The `RobustToolbox` submodule itself points at the official
`space-wizards/RobustToolbox.git` (not a fork). It is just an engine
**version pin**.

## The `no-submodule-update` guard

`.github/workflows/no-submodule-update.yml` **fails any PR whose diff
touches the `RobustToolbox` path** (`exit 1`). Consequence:

- A PR may **never** change the pinned engine commit relative to its base.
- Engine version is effectively frozen; it is only bumped through a
  separate, dedicated flow — not through content PRs or upstream merges.

So when merging `upstream/master` (which may carry a newer engine pin),
the merge **must keep `origin/master`'s engine pointer**, discarding the
upstream engine bump. Verify with:

```
git ls-tree HEAD RobustToolbox
git ls-tree origin/master RobustToolbox   # must be identical
```

## The coupled slnx pitfall

`SpaceStation14.slnx` (the solution file at repo root, tracked content)
lists every project, **including the engine's projects**. Newer engine
versions add/remove projects (e.g. `Robust.Shared.EntitySystemSubscriptionsGenerator`
exists only in **v282+**, not v277).

An upstream merge can pull in the newer engine's **slnx layout** while the
engine **pin** is (correctly) kept at the older version. Result: the slnx
references a `.csproj` that does not exist in the pinned engine →
`dotnet build` fails → the **YAML linter fails** (it needs `--no-build`
after a successful build). This looks like a "linter" problem but the
root cause is the slnx/engine mismatch.

Fix: make the slnx match the pinned engine. The canonical version is the
pre-merge slnx from the base side, e.g.:

```
git checkout <pre-merge-commit> -- SpaceStation14.slnx
```

There is no content-side engine version pin: `Directory.Packages.props`
just `<Import>`s `RobustToolbox/Directory.Packages.props`, so CI builds
against whatever commit the submodule is checked out at (the pin). Hence
the slnx must match the pin.

## Fixing a botched upstream merge (recipe)

1. Ensure `HEAD:RobustToolbox == origin/master:RobustToolbox`. If the
   merge recorded the wrong pin, the fix goes **forward** (a new commit),
   not via rebase/force-push, since the merge is already on `origin/master`.
2. If the working tree submodule is dirty (checked out at the wrong
   version): `git restore --staged RobustToolbox && git submodule update --init RobustToolbox`.
3. Restore `SpaceStation14.slnx` to the base/pre-merge layout so it matches
   the pinned engine (drop any newer-engine-only project entries).
4. Run `python RUN_THIS.py` (→ `BuildChecker/git_helper.py`): installs
   hooks and runs `git submodule update --init --recursive`. It checks out
   the **pinned** commit and no longer rewrites the slnx
   (`reset_solution()` is commented out). It early-returns on
   `GITHUB_ACTIONS`.

## Hooks

`.git/hooks/{post-checkout,post-merge}` both run
`BuildChecker/git_helper.py --quiet`, which updates submodules to the pin
and installs hooks. Note: an IDE watching the solution may rewrite the
slnx on disk when the engine checkout changes — prefer restoring the slnx
from git over trusting an editor regeneration (it may add spurious empty
`<Folder>` entries).

## Reference incident (2026-07-16)

Merge `46ac2a137b` from `upstream` kept engine pin at **v277.0.0**
(`08a3d120`, correct) but carried the **v282 slnx layout** referencing
`Robust.Shared.EntitySystemSubscriptionsGenerator`. Working tree was also
left dirty at v282 (`960edb32c`). Fix: reset submodule to v277, drop the
v282-only slnx line (commit `0bdef28a7a`). No engine-pin change → passes
`no-submodule-update`; slnx matches v277 → build/linter pass.

See also [[testing.md]] for CI strategy.
