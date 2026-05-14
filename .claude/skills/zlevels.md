# ZLevels (Z-уровни)

Система вертикальных уровней, портированная из CrystallEdge (MIT).

## Архитектура

- Код: `Content.Server/_Respiral/ZLevels/Core/CEZLevelsSystem.cs`, компоненты с префиксом `CE`
- При `StationPostInitEvent` система загружает карты из `CEStationZLevelsComponent.MapsBelow` / `MapsAbove`
- Каждая карта добавляется как отдельный grid к станции через `StationSystem.AddGridToStation()`
- Все Z-уровни объединяются в `ZNetwork` с соответствующими уровнями глубины

## Прототипы

- Определяются в YAML-файле карты (например `verdant_vale.yml`)
- Компонент `CEStationZLevels` задаёт списки карт выше/ниже основной
- Также объявляется `zMap` прототип со всеми картами для Z-навигации

## Известные проблемы и решения

### `Required properties not provided for` в логах Serilog
**Причина:** `StationNameSetup.mapNameTemplate` содержит плейсхолдеры `{0}`, `{1}` без задания `nameGenerator`. Имя станции остаётся неформатированным. При логировании `StationSystem.AddGridToStation` (строка 350) Serilog интерпретирует `{0}` и `{1}` как именованные параметры шаблона.
**Решение:** Либо убрать плейсхолдеры из `mapNameTemplate`, либо добавить `nameGenerator` в прототип.
