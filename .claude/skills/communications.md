# Communications Console (Средневековье)

## Связующий магический шар

**Entity:** `MedievalConnectingMagicBall`
**Detail:** `MedievalConnectingMagicBallDetail`

Средневековая замена консоли коммуникаций. Позволяет отправлять объявления на станцию без электричества.

### Ключевые решения

- Наследуется от `MedievalConstructibleMachine` + `MedievalBaseMachine` (стандартный граф `MedievalMachine`)
- Работает **без электричества** (нет `ApcPowerReceiver`, `Computer`, `PowerState`)
- `canShuttle: false` — шаттл не вызвать (средневековье)
- Нет `AccessReader` — шар доступен всем
- Спрайт: `_Respiral/Structures/Machines/connecting_magic_ball.rsi` (анимация `running`)
- Свечение: фиолетовое `#7b68ee`

### Крафт

Использует стандартный граф `MedievalMachine`:
1. Поставить `MedievalUnfinishedMachineFrame` (камень в составе каркаса)
2. Добавить `MedievalManaCrystal` (магический кристалл)
3. Вставить `MedievalConnectingMagicBallDetail` (деталь)
4. Закрутить (Screwing)

### Деталь

`MedievalConnectingMagicBallDetail` — наследует `MedievalBaseMachineDetail`, содержит `MachineBoard` с `prototype: MedievalConnectingMagicBall`. Состав: `MedievalProcessedStone: 300`, `MedievalRawManaCrystal: 200`.

### Файлы

| Файл | Содержимое |
|---|---|
| `Entities/Structures/Machine/connecting_magic_ball.yml` | Прототип сущности |
| `Entities/Objects/Parts/production.yml` | Деталь (MedievalConnectingMagicBallDetail) |
