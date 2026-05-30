# Bureaucracy (Бумага, свитки, штампы)

## Иерархия прототипов

```
BasePaper (upstream, abstract)
└── MedievalBasePaper (abstract, добавляет MedievalPhysicalPriced)
    ├── MedievalPaper (записываемая бумага)
    │   └── MedievalPaperWritten (для маппинга, HideSpawnMenu)
    ├── MedievalScroll (свиток пергамента)
    │   └── MedievalScrollMagic (магический свиток)
    └── MedievalPassport (паспорт)

BoxBase (upstream)
└── MedievalFolderBlue (папка-хранилище)
    └── MedievalFolderRed

RubberStampBase (upstream)
├── MedievalStampBank (bank_on_paper, нейтральная)
├── MedievalStampApproved (approved_on_paper, зелёная)
└── MedievalStampDenied (denied_on_paper, красная)
```

## Sprite RSI

`_Respiral/Objects/Items/Bureaucracy/paper.rsi`

| State | Назначение |
|---|---|
| `paper` | Чистый лист |
| `paper_filled` | Лист с текстом (Writing layer) |
| `bank_on_paper` | Печать на бумаге (нейтральная, поддерживает color) |
| `approved_on_paper` | Печать одобрения |
| `denied_on_paper` | Печать отказа |
| `folder_blue` | Синяя папка |
| `folder_red` | Красная папка |
| `passport` | Паспорт |
| `scrolls` | Свиток |
| `magic` | Магический свиток |

## Паттерн Paper (записываемая сущность)

Для создания записываемой бумаги нужны:
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

### Слои спрайта
```yaml
layers:
- state: paper                    # Базовый вид
- state: paper_filled             # Текст (PaperVisualLayers.Writing), visible: false
  map: ["enum.PaperVisualLayers.Writing"]
  visible: false
- state: bank_on_paper            # Штамп (PaperVisualLayers.Stamp), visible: false
  map: ["enum.PaperVisualLayers.Stamp"]
  visible: false
```

## Система штампов

`StampComponent` на сущности штампа определяет:
- `stampState` — state из RSI бумаги, отображаемый на спрайте бумаги при штамповке
- `stampedColor` — цвет печати в UI бумаги
- `stampedName` — loc-строка, отображаемая при осмотре бумаги

Штамп-стейты должны быть в RSI **бумаги** (не штампа), т.к. `PaperVisualizerSystem` меняет state слоя `PaperVisualLayers.Stamp`.

## Папки (Storage)

Папки наследуют `BoxBase` (grid storage), фильтруют по тегу `Document`. Средневековые папки — цельные спрайты (не colormap + base как оригинальные).

## Файлы
| Путь | Содержимое |
|---|---|
| `Resources/Prototypes/_Respiral/Entities/Objects/Bureaucracy/paper.yml` | Все прототипы бумаги, свитков, папок, штампов |
| `Resources/Locale/ru-RU/_Respiral/paper.ftl` | Локализация stampedName |
| `Resources/Textures/_Respiral/Objects/Items/Bureaucracy/paper.rsi` | Все спрайты |
