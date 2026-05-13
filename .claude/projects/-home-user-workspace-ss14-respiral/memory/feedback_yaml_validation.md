---
name: YAML validation method
description: Для проверки YAML-прототипов использовать YAML-линтер, не dotnet build
type: feedback
---

Для проверки прототипов (YAML) использовать YAML-линтер, НЕ `dotnet build`.

**Команда:** `dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj`

`dotnet build` — только при изменении .cs кода.

**Why:** dotnet build проверяет только компиляцию C#, а не валидность прототипов. YAML-линтер проверяет ссылки на материалы, стеки, сущности, спрайты.

**How to apply:** после изменения/создания .yml файлов запускать линтер, а не билд.
