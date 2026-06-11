# Testing & CI

## Интеграционные тесты (Content.IntegrationTests)

- Тестовый пул (`PoolManager`) **всегда грузит весь контент** (server+client пара монтируют все
  прототипы из `Resources/Prototypes`). Загрузку нельзя ограничить подпапкой: Respiral-прототипы
  наследуются (`parent:`) от апстрима и ссылаются на него. `PoolSettings` даёт лишь `NoLoadContent`
  (совсем без контента), промежуточного «только эта папка» нет.
- После загрузки прототип **не хранит исходную папку/файл**. `EnumeratePrototypes<EntityPrototype>()`
  — плоский список ID. Фильтровать sweep-тесты «по папке ресурсов» на рантайме нельзя.
- Чтобы определить принадлежность к Respiral на рантайме, источник истины — **папка**, а не префикс ID:
  через `IResourceManager.ContentFindFiles(new ResPath("/Prototypes/_Respiral/"))` + парсинг YAML
  собираем множество `id:` для `type: entity`. Префиксы ID ненадёжны (см. ниже).

### `--filter` (VSTest/NUnit)
- Отбирает **тестовые методы по имени** (`FullyQualifiedName~...`), НЕ умеет залезать внутрь
  метода-цикла (`EntityTest` перебирает все прототипы — его можно только включить/исключить целиком).
- Sweep-тестов, перебирающих весь контент, в репо ~37 (EntityTest, PostMapInitTest, StationPowerTests,
  PrototypeSaveTest, CargoTest, LatheTest и т.д.) — исключать их поимённо хрупко.

### Стратегия CI (build-test-debug.yml)
**Два отдельных workflow-файла** (`build-test-debug.yml` НЕ трогать — это апстрим-гейт):
1. `build-test-debug.yml` — **оригинальный**, без изменений: full build + все апстрим-тесты.
2. `respiral-test.yml` — **только Respiral**: build → `validate_rsis.py Resources/Textures/_Respiral/`
   → интеграционные тесты `--filter "FullyQualifiedName~Tests._Respiral"`.

`continue-on-error`/неблокирующий апстрим в одном файле — устаревший вариант; теперь разделение по файлам.
Required-статус в branch protection назначать на `respiral-test.yml`, апстримный — по желанию.

### Respiral-тесты (`Content.IntegrationTests/Tests/_Respiral/`, ns `...Tests._Respiral`)
- `RespiralEntityTest.SpawnAndDeleteAllRespiralEntities` — спавн/удаление всех сущностей из папки
  `/Prototypes/_Respiral/` (множество ID из папки через `IResourceManager`, не из префикса).
- `RespiralMapTest.RespiralMapsLoadableTest` — грузит каждую карту из `/Maps/_Respiral/`
  (`GameDataScrounger.FilesInDirectoryInVfs` + `MapLoaderSystem.TryLoadGeneric`, `InitializeMaps=true`).
- Текстуры/RSI валидируются НЕ интеграционным тестом, а шагом `validate_rsis.py` в workflow
  (формат `.rsi/meta.json`, размеры PNG). Скоуп — `Resources/Textures/_Respiral/`.
- API карт: `MapLoaderSystem` в `Robust.Shared.EntitySerialization.Systems`, `MapLoadOptions`/
  `DeserializationOptions` в `Robust.Shared.EntitySerialization`. `GameMapPrototype`-карты грузятся
  через `GameTicker.LoadGameMap`; произвольные map-файлы — через `TryLoadMap`/`TryLoadGeneric`.

## Префиксы ID Respiral
- Основной префикс — `Medieval` (PascalCase compound: `MedievalRoofWooden`, `MedievalWoodenSign`).
- НО много сущностей без этого префикса: `Bush*`, `Organ*`, `Mob*`, `Food*`, `Stone*`, `Gallows*` и др.
  → опознавать Respiral-контент по префиксу ID **нельзя**, только по папке.
- Исправленные опечатки (2026-06): `MedivalWoodenSign*`→`Medieval...`, `MedievaHammerAmmoRecipe`→`Medieval...`,
  `Medievalvorosnik`→`MedievalVorosnik` (PascalCase; `воросник` — растение, транслитерация верна).
- Namespacing «голых» id (2026-06): все Respiral-специфичные прототипы без префикса приведены к `Medieval<Name>`
  (PascalCase). Затронуто: 12 `flavor` (`mineral`→`MedievalMineral` и т.д.), `parallax` `Water/Stone/Black`,
  `npcFaction` `TownPeople`, `soundCollection` `LearnBook`, `entity` `Gallows`. ftl-ключи `flavor-base-*`
  НЕ менялись (id ≠ loc-key, ссылки на ключи валидны).
- ВАЖНО при переименовании коротких id (`dry`, `wheat`, `Stone`): ссылки менять только в их контексте
  (`flavors:`-блоки, `parallax:`-поле), а не слепым substring-replace — иначе заденет другие слова.
- НЕ трогать ZLevels/CE-неймспейс (`ZLevelEye`, `ZBlur`, `ActionZLevel*`) — это MIT-порт со своей конвенцией `CE`.

## Замечания
- `TestStationStartingPowerWindow` помечен `[Explicit]` → всегда `Skipped` в обычном прогоне. Если в
  логе после него `Error: The operation was canceled` — это не он упал, а процесс убили (OOM/таймаут);
  это просто последняя строка перед обрывом stdout.
- Валидацию YAML делать YAML-линтером, не `dotnet build` (см. память проекта).
