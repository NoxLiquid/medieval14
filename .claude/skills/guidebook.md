# Guidebook (in-game guide) — _Respiral

In-game guidebook (opened with the `OpenGuidebook` keybind). SS14's `guideEntry` prototype system:
a tree of entries, each pointing at a `<Document>` markup file.

## Where things live
- **Entry prototypes:** `Resources/Prototypes/_Respiral/Guidebook/guidebook.yml` (`- type: guideEntry`).
- **Content files:** `Resources/ServerInfo/Guidebook/_Respiral/*.xml`.
- **Entry names (loc):** `Resources/Locale/{ru-RU,en-US}/_Respiral/guidebook.ftl` — key = the entry's
  `name:` field (e.g. `guide-entry-medieval-combat`). Both languages must be kept in parity.

## Tree structure
- The tree is built via `children:` on each `guideEntry`. Any entry that is **not** a child of another
  becomes a **root tab**. `Medieval` (`priority: -10`, sorts high) is the Respiral root tab.
- To add a page: create the `.xml`, add a `guideEntry` (id/name/text[/children]), reference its id in the
  parent's `children:`, and add the `name` key to both `.ftl` files.

### Current tree (under `Medieval`)
```
Medieval (Средневековье)
├─ MedievalIdentity      — Identity.xml
├─ MedievalSkills        — Skills.xml
│  └─ MedievalSkillsLiteracy — SkillsLiteracy.xml
└─ MedievalCombat        — Combat.xml   (ветка «Бой, здоровье и возрождение», added 2026-09-05)
   ├─ MedievalCombatDamage — CombatDamage.xml
   ├─ MedievalArmor        — Armor.xml
   └─ MedievalDeathRespawn — DeathRespawn.xml
```

## `<Document>` markup
Outer must be well-formed XML (validate: `python3 -c "import xml.etree.ElementTree as ET; ET.parse(p)"`).
Inside, mix of Markdown + RichText tags:
- `#` / `##` headings; `[bold]`/`[italic]`/`[color=red]…[/color]`.
- `[textlink="Текст" link="EntryId"/]` — jump to another guide entry (id must exist).
- `[keybind="OpenGuidebook"]` — renders the bound key.
- `<GuideEntityEmbed Entity="EntityProtoId" Caption="…"/>` — shows an entity sprite+name. **The
  EntityProtoId must exist** or the doc fails to load — verify every id before shipping. Wrap rows of
  embeds in `<Box>…</Box>`.

## Content roadmap (proposed, from `.claude/skills/` domains)
Persona/society (Identity✅, Skills✅, Professions, Bureaucracy) · Combat/health/rebirth✅ · Crafting &
materials (materials/wear, construction, lathes, tailoring) · Magic (mana, spells) · World & husbandry
(flora, mounts/hitch/rope, locks, trade, communications, round goal). Write player-facing, immersive
Russian second-person prose matching the existing entries; keep en-US loc names in parity.

## Validation
Static: XML well-formed; every `link=`/`children:` id defined; every `GuideEntityEmbed Entity=` exists;
loc keys present in ru+en. Full: the guidebook document-parsing integration test (needs a build).
