# Guidebook (in-game guide) — _Respiral

In-game guidebook (opened with the `OpenGuidebook` keybind). SS14's `guideEntry` prototype system:
a tree of entries, each pointing at a `<Document>` markup file.

## Where things live
- **Entry prototypes:** `Resources/Prototypes/_Respiral/Guidebook/guidebook.yml` (`- type: guideEntry`).
- **Content files:** `Resources/ServerInfo/Guidebook/_Respiral/<Branch>/*.xml` — organized into per-branch
  subfolders (`Identity/`, `Skills/`, `Combat/`, `Crafting/`, `World/`); the root `Medieval.xml` stays at
  the top level. A `guideEntry`'s `text:` path must match the file's subfolder. Nothing but `guidebook.yml`
  references these paths, so moving a file = update its `text:` line only.
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
├─ MedievalCombat        — Combat.xml   (ветка «Бой, здоровье и возрождение», added 2026-09-05)
│  ├─ MedievalCombatDamage — CombatDamage.xml
│  ├─ MedievalArmor        — Armor.xml
│  └─ MedievalDeathRespawn — DeathRespawn.xml
├─ MedievalCrafting      — Crafting.xml (ветка «Ремёсла и материалы», added 2026-09-05)
│  ├─ MedievalMaterials    — Materials.xml   (материалы, тиры, прочность/износ)
│  ├─ MedievalStations     — Stations.xml    (лейт-верстаки: переработка vs изготовление)
│  ├─ MedievalBuilding     — Building.xml    (строительство: меню OpenCraftingMenu + призраки)
│  └─ MedievalTailoring    — Tailoring.xml   (дубильня→ткацкий→швейный)
└─ MedievalWorld         — World.xml    (ветка «Мир и хозяйство», added 2026-09-05)
   ├─ MedievalFlora        — Flora.xml          (лес/рубка→брёвна, сбор, охота)
   ├─ MedievalRiding       — Riding.xml         (седло, повозка-hitch, верёвка-rope)
   ├─ MedievalLocks        — Locks.xml          (замок/ключ/отмычка/дубликат, «Под замком»)
   ├─ MedievalTrading      — Trade.xml          (монеты, кошель, торговые знаки-pallet)
   └─ MedievalTidings      — Communications.xml (магический шар + указ виконта)
```

Content files are stored per-branch on disk (`Combat/`, `Crafting/`, `World/`, `Identity/`, `Skills/`;
`Medieval.xml` at root) — done 2026-09-05. `guidebook.yml` is still a single file. The `children:` tree
is the logical clustering; don't add mid-level super-nodes (extra clicks for ~6 branches).

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
