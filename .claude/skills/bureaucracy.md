# Bureaucracy (Paper, scrolls, stamps)

## Prototype hierarchy

```
BasePaper (upstream, abstract)
└── MedievalBasePaper (abstract, adds MedievalPhysicalPriced)
    ├── MedievalPaper (writable paper)
    │   └── MedievalPaperWritten (for mapping, HideSpawnMenu)
    ├── MedievalScroll (parchment scroll)
    │   └── MedievalScrollMagic (magic scroll)
    └── MedievalPassport (passport, + MedievalCredential/Visualizer)   # implemented, see identity.md

BoxBase (upstream)
└── MedievalFolderBlue (storage folder)
    └── MedievalFolderRed

RubberStampBase (upstream)
├── MedievalStampBank (bank_on_paper, neutral)
├── MedievalStampApproved (approved_on_paper, green)
└── MedievalStampDenied (denied_on_paper, red)
```

## Sprite RSI

`_Respiral/Objects/Items/Bureaucracy/paper.rsi` — Respiral's own copy of the upstream
`Objects/Misc/bureaucracy.rsi` (CC-BY-SA-3.0, copyright = base tgstation attribution;
special attributions removed together with their states) + own passport sprites:

| State | Purpose |
|---|---|
| `paper` | Base sheet of ordinary paper |
| `paper_words` | Text of ordinary paper (layer `PaperVisualLayers.Writing`) |
| `paper_stamp-generic` | Neutral stamp (bank, house seal) |
| `paper_stamp-ok` | Approval stamp |
| `paper_stamp-deny` | Denial stamp |
| `passport_close` | Folded credential, cover; visualizer's `ClosedState` |
| `passport_icon` | Seal icon over the cover (layer `MedievalCredentialVisualLayers.Icon`) |
| `passport_open` | Unfolded credential; visualizer's `OpenState` |
| `passport_open_words` | Text of unfolded credential (layer `PaperVisualLayers.Writing`) |

The passport has 4 of its own states. Layer-visibility logic (icon only when folded;
text and stamp only when unfolded) lives in `MedievalCredentialVisualizerSystem`, see identity.md.

## Paper pattern (writable entity)

To create writable paper you need:
```yaml
- type: Paper
- type: PaperLabelType
- type: ActivatableUI
  key: enum.PaperUiKey.Key
  requiresComplex: false
- type: UserInterface
  interfaces:
    enum.PaperUiKey.Key:
      type: PaperBoundUserInterface
- type: PaperVisuals
```

### Sprite layers
```yaml
layers:
- state: paper                    # Base appearance
- state: paper_filled             # Text (PaperVisualLayers.Writing), visible: false
  map: ["enum.PaperVisualLayers.Writing"]
  visible: false
- state: bank_on_paper            # Stamp (PaperVisualLayers.Stamp), visible: false
  map: ["enum.PaperVisualLayers.Stamp"]
  visible: false
```

## Stamp system

`StampComponent` on the stamp entity defines:
- `stampState` — state from the paper's RSI shown on the paper sprite when stamped
- `stampedColor` — stamp color in the paper UI
- `stampedName` — loc string shown when examining the paper

Stamp states must be in the **paper's** RSI (not the stamp's), because `PaperVisualizerSystem` changes the state of the `PaperVisualLayers.Stamp` layer.

## Faxable

`MedievalPaper` carries `FaxableObject` so it can be inserted into / sent through a
`MedievalTeleportTable` (the reskinned fax). See [[round_goal]].

## Folders (Storage)

Folders inherit `BoxBase` (grid storage), filter by the `Document` tag. Medieval folders are single-piece sprites (not colormap + base like the originals).

## Paper as a lathe material + Passport Office

`MedievalBasePaper` carries `PhysicalComposition { MedievalPaper: 100 }` and the tag
`MedievalPassportOfficeMaterialStorage`, so every paper/scroll/passport (Tag lists are *replaced*
on inheritance, and no child redefines its own Tag) can be fed into the Passport Office as raw
paper material.

- **Material** `MedievalPaper` (`materials.yml`): `stackEntity: MedievalPaper` (the writable sheet;
  it has no `Stack`, which is fine — sheet volume is read from that entity's composition = 100, and
  material ejection spawns individual sheets via `SpawnMultipleAtPosition`, which no-ops the count on
  non-stack entities). Loc `medieval-materials-paper` (materials.ftl). Different prototype *kind* from
  the `MedievalPaper` entity, so the shared id does not collide (same trick as `MedievalCloth`).
- **Machine** `MedievalPassportOffice` (`Structures/Machine/lathe.yml`, sprite
  `_Respiral/Structures/Machines/passport_office.rsi`, states icon/inserting/building). Gated by
  `NeededSkillForInteract: MedievalBureaucracy`. MaterialStorage whitelist tag
  `MedievalPassportOfficeMaterialStorage`. Static pack `MedievalPassportOffice`.
- **Board/detail** `MedievalPassportOfficeDetail` (wood, `Objects/Parts/production.yml`), recipe
  `MedievalPassportOfficeDetailRecipe` crafted at the Carpenter Table.
- **Passport recipe** `MedievalPassportRecipe` (result `MedievalPassport`, consumes `MedievalPaper: 100`),
  category `MedievalBureaucracyCategory` (loc `lathe-medieval-category-bureaucracy`), base recipe
  `MedievalBaseBureaucracyRecipe`.
- **Skill** `MedievalBureaucracy` (`Skill/skill.yml`) — granted to `MedievalCitySteward` (progress 1);
  only entities with the skill can use the office. No skill book yet (learn-by-book gap). See
  [[professions]] and [[skill_system]].

## Files
| Path | Contents |
|---|---|
| `Resources/Prototypes/_Respiral/Entities/Objects/Bureaucracy/paper.yml` | All paper, scroll, folder, stamp prototypes |
| `Resources/Locale/ru-RU/_Respiral/paper.ftl` | Localization of stampedName |
| `Resources/Textures/_Respiral/Objects/Items/Bureaucracy/paper.rsi` | All sprites |
