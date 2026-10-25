<div class="header" align="center">
<img alt="Space Station 14" width="880" height="300" src="https://raw.githubusercontent.com/space-wizards/asset-dump/de329a7898bb716b9d5ba9a0cd07f38e61f1ed05/github-logo.svg">
</div>

### **Respiral** — фэнтезийный контент-пак для Space Station 14.

_Главная идея проекта — цикл жизни, смерть и повторение._
Это попытка создать фэнтезийную сборку для SS14 в духе других аналогичных проектов в SS13 и не только.

---

## 🔗 Ссылки

[Сборка Respiral](https://github.com/respiral-tree/ss14-respiral) | [Основной репозиторий SS14](https://github.com/space-wizards/space-station-14) | [Официальная документация SS14](https://docs.spacestation14.com/)

---

## 📚 Документация

Вся базовая информация о движке, контенте и разработке доступна в [официальной документации SS14](https://docs.spacestation14.com/). Там же есть инструкции для начинающих разработчиков и моддеров.

---

## 🤝 Контрибьют

Мы приветствуем вклад от любого желающего. Откройте issue или сделайте pull request в репозитории `ss14-respiral`. Не стесняйтесь просить помощи — сообщество подскажет, как начать.

Перед отправкой PR убедитесь, что изменения соответствуют официальным [рекомендациям по контрибьюту](https://docs.spacestation14.com/en/general-development/codebase-info/pull-request-guidelines.html).

---

## ⚙️ Сборка

1. Склонируйте репозиторий локально:
```shell
git clone https://github.com/respiral-tree/ss14-respiral.git
```
2. Запустите `RUN_THIS.py` для инициализации подмодулей и скачивания движка:
```shell
python RUN_THIS.py
```
3. Соберите проект через вашу IDE (Visual Studio / Rider) или командой `dotnet build`.

Подробная инструкция по сборке и настройке — в [официальной документации SS14](https://docs.spacestation14.com/en/general-development/setup.html).

---

## 📄 Лицензия

**Делайте с кодом что хотите.** Собственный код и данные проекта — каталоги `_Respiral` и фрагменты с тегом
`Respiral` в остальных файлах, написанные KloopRE, — переданы в общественное достояние по
[**Unlicense**](LICENSE.TXT).

Исключения — эти части сохраняют свои лицензии:
- Код Space Station 14 (upstream), RobustToolbox и каталоги `_Corvax` — [**MIT**](MIT_LICENSE.TXT); при
  распространении сохраняйте уведомление MIT.
- Каталоги `ZLevels`, изменения с тегом `Respiral-ZLeveling` и файлы `_CE` с заголовком «This file is
  sublicensed under MIT License» — [**MIT**](MIT_LICENSE.TXT), портировано с
  [CrystallEdge](https://github.com/crystallpunk-14/crystall-edge).
- Остальной код в каталогах `_CE` используется с разрешения автора CrystallEdge (TheShuEd) и в общественное
  достояние не передаётся; для использования в других проектах нужно его разрешение.
- Код, внесённый в `_Respiral` другими участниками, остаётся на условиях, на которых был внесён (GNU GPL v3).
- Ассеты — по лицензиям, указанным в их `meta.json` / `attributions.yml` (см. ниже).

Большинство ассетов лицензированы под **CC-BY-SA 3.0**, если не указано иное. Метаданные с лицензией и авторством находятся в файлах `*.rsi/meta.json`.

> [!NOTE]
> ⚠️ Обратите внимание: некоторые ассеты могут быть под **CC-BY-NC-SA 3.0** (некоммерческая лицензия). Такие ресурсы нужно заменить или удалить при необходимости использования в коммерческих проектах.
