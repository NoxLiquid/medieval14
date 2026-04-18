# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Respiral** is a fantasy-themed content pack for Space Station 14 (SS14). The core theme is life cycles, death, and rebirth. It is built on the Robust Toolbox engine (C#/.NET 10).

## Build & Run

```shell
# First-time setup (initializes submodules, downloads engine)
python RUN_THIS.py

# Build
dotnet build

# Run client
dotnet run --project Content.Client
# or: runclient.bat (Windows)

# Run server
dotnet run --project Content.Server
# or: runserver.bat (Windows)
```

## Tests

```shell
dotnet test Content.Tests
dotnet test Content.IntegrationTests
dotnet run --project Content.Benchmarks
```

## Code Conventions

- C#, .NET 10, nullable reference types enabled (`<Nullable>enable</Nullable>`)
- 4-space indentation, max line length 120 (enforced via `.editorconfig`)
- `RA0032` and nullable warnings are treated as errors

## Architecture

### Content split

All game logic is split across three projects following the SS14 pattern:

| Project | Purpose |
|---|---|
| `Content.Shared` | Components, prototypes, events, shared systems (runs on both client and server) |
| `Content.Server` | Server-only systems (gameplay logic, NPC AI, map management) |
| `Content.Client` | Client-only systems (UI, visuals, predictions) |

### Respiral-specific code

All Respiral additions live under `_Respiral/` subdirectories within each project, keeping them separate from upstream SS14 code. Key systems:

- **ZLevels** (`_Respiral/ZLevels/`) — Multi-Z-level maps (vertical floors/layers), ported from CrystallEdge under MIT. Commands prefixed `CE`, components prefixed `CE`.
- **Blacksmithing** (`_Respiral/Blacksmithing/`) — Forge/anvil crafting with heat, quality, and quenching mechanics.
- **Skills** (`_Respiral/Skill/`) — Skill progression system; skills learned by performing actions (melee attacks, item use, etc.).
- **Wear** (`_Respiral/Wear/`) — Item durability/degradation over use; clothing damage distribution.
- **Lock** (`_Respiral/Lock/`) — Medieval key/lock system distinct from the standard SS14 lock.
- **Burning/Temperature** (`_Respiral/Burning/`, `_Respiral/Temperature/`) — Fuel, heat sources, heated items.
- **Flora** (`_Respiral/Flora/`) — Harvestable plant entities.
- **NPC** (`_Respiral/NPC/`) — Medieval NPC behaviors: herbivore fear, flee operators.

### Resources / Prototypes

- `Resources/Prototypes/_Respiral/` — All YAML entity/prototype definitions for Respiral content.
- `Resources/Textures/_Respiral/` — Sprites and textures.
- `.rsi/meta.json` files store license and attribution metadata for all sprite assets.

### ZLevels licensing note

Code under `ZLevels/` directories and tagged `Respiral-ZLeveling` is MIT-licensed (ported from CrystallEdge). Everything else under `_Respiral` is GPL v3. Upstream SS14 code is MIT.

## Official Documentation

Full engine/content dev docs: https://docs.spacestation14.com/
