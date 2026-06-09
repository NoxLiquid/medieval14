# Локализация (FTL)

## Где лежит

- `Resources/Locale/<culture>/...` — FTL-файлы по культурам (`en-US`, `ru-RU`).
- Структура подкаталогов в `ru-RU` зеркалит `en-US`.

## Главное правило: паритет с en-US

При апстрим-мерже текст сообщения в `en-US` может поменять **схему аргументов**, а
не только слова. Перевод `ru-RU` обязан повторять **форму аргументов/функций**
английского сообщения, иначе FTL на рантайме кидает `Unknown variable: $...`, и
интеграционные тесты падают (пул сервера репортит это как `[ERRO] loc: "ru-RU"/...`).

Тест проверяет именно рантайм-резолв строк, а не сам YAML — `dotnet build` это не
ловит. Сверять руками.

### Проверки

Паритет message ID между культурами:

```sh
diff \
  <(grep -oE '^[a-z0-9-]+ =' Resources/Locale/en-US/<path>.ftl | sort) \
  <(grep -oE '^[a-z0-9-]+ =' Resources/Locale/ru-RU/<path>.ftl | sort)
```

Должно быть пусто. Если в ru не хватает ID (новые сообщения апстрима) — добавить и
перевести; лишние ID в ru — удалить.

Проверка отсутствия устаревших переменных после правки:

```sh
grep -oE '\$[a-z]+' Resources/Locale/ru-RU/<path>.ftl | sort -u
```

Набор `$`-переменных должен совпадать с en-US (с поправкой только на статический текст).

## Кейс: ion-storm (AI-законы)

`Resources/Locale/{en-US,ru-RU}/station-events/events/ion-storm.ftl`.

Апстрим (Wizard's Den) переписал систему ион-штормовых законов. Раньше каждое
сообщение получало много отдельных переменных (`$who`, `$joined`, `$subjects`,
`$adjective`, `$thing`, ...). Теперь каждому закону передаётся **один аргумент
`$ion`**, а динамические части резолвятся через FTL-term-функции:
`ION-WHO($ion)`, `ION-NUMBER-BASE($ion)`, `ION-NUMBER-MOD($ion)`,
`ION-ADJECTIVE($ion)`, `ION-SUBJECT($ion)`, `ION-THING($ion)`, `ION-MUST($ion)`,
`ION-PLURAL($ion)`, `ION-PART($ion)` и т.д.

Функции регистрируются для `DefaultCulture` в
`Content.Server/Silicons/Laws/IonLawLocalizationSystem.cs` (значения берутся из
`IonLawSystem.GetOrGenerateValue`). Аргумент `$ion` функции игнорируют (`_ =>`),
он лишь маркер.

Перевод `ru-RU` был оставлен на старой схеме переменных → `Unknown variable`.
Фикс: переписать `ru-RU/.../ion-storm.ftl`, зеркаля структуру `en-US` —
сохранить вызовы `{ION-X($ion)}` и ссылки на term `{ion-storm-part}`, перевести
только статический текст и ветки select-выражений (`{ION-PLURAL($ion) -> [true] ТРЕБУЮТ *[false] ТРЕБУЕТ}`).
Хелпер-термы (`ion-storm-you`, `ion-storm-the-job`, `ion-storm-adjective-things`,
`ion-storm-x-and-y`) сохраняют свои исходные аргументы — их не трогать.

Новые сообщения, которые отсутствовали в ru и были добавлены: `ion-storm-law-who-dagd`
и блок `ion-law-error-*` (фолбэк-строки на случай сбоя генерации закона).
