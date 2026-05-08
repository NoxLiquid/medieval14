# Trade Console (Торговая табличка)

## Overview
Средневековая торговая система — таблички для заказа и продажи товаров.
Использует существующую систему `CargoOrderConsole` + `CargoPallet` + `CargoPalletConsole` без кастомного C# (кроме маркера TradeStation).

## Architecture

### Покупка (заказ товаров)
- `MedievalTradeConsole` — табличка с `CargoOrderConsole` для оформления заказов
- `MedievalTradePalletBuy` — палета с `CargoPallet (palletType: buy)` — сюда спавнятся заказанные товары
- Доставка через `TryFulfillOrder()` — система ищет гриды с `TradeStationComponent`, находит свободные Buy-палеты и спавнит товары на них

### Продажа
- `MedievalTradePalletSell` — палета с `CargoPallet (palletType: sell)` — положить товар для продажи
- `MedievalTradePalletConsole` — табличка с `CargoPalletConsole` — оценка и продажа товаров на Sell-палетах **на том же гриде**

### TradeStation на гридах
- `MedievalTradeStationComponent` — маркер на станции (`Content.Shared/_Respiral/Trade/`)
- `MedievalTradeStationSystem` — при `StationPostInitEvent` добавляет `TradeStationComponent` на все гриды станции (`Content.Server/_Respiral/Trade/`)
- Без `TradeStationComponent` на гриде `TryFulfillOrder()` не найдёт палеты и товары не доставятся

## Key Design Decisions
- **НЕ используется DeviceLink/телепад** — доставка через Trade Station палеты (TryFulfillOrder), не через телепортацию
- Питание не требуется: `StaticPowerSystem.IsPowered()` возвращает `true` если нет `ApcPowerReceiver`
- Отдельный `CargoMarket` (`MedievalMarket`) и `CargoAccount` (`MedievalTrade`)

## Механизм доставки (как работает в коде)
1. Игрок одобряет заказ через `MedievalTradeConsole`
2. Поднимается `FulfillCargoOrderEvent`
3. Телепад не обрабатывает (нет DeviceLink) → fallback на `TryFulfillOrder()`
4. `GetTradeStations()` ищет гриды станции с `TradeStationComponent`
5. `GetCargoPallets(grid, Buy)` находит Buy-палеты
6. `GetFreeCargoPallets()` фильтрует незанятые
7. `FulfillOrder()` спавнит товары на свободных палетах

## File Locations
- Сущности: `Resources/Prototypes/_Respiral/Entities/Structures/trade_console.yml`
- Рынок + товары: `Resources/Prototypes/_Respiral/Catalog/medieval_trade.yml`
- Станция: `Resources/Prototypes/_Respiral/Stations/base.yml`
- Маркер: `Content.Server/_Respiral/Trade/Components/MedievalTradeStationComponent.cs`
- Система: `Content.Server/_Respiral/Trade/MedievalTradeStationSystem.cs`
- Ценообразование: `Content.Server/_Respiral/Pricing/MedievalPhysicalPricedComponent.cs`
- Локализация RU: `Resources/Locale/ru-RU/_Respiral/cargo/medieval-trade.ftl`
- Локализация EN: `Resources/Locale/en-US/_Respiral/cargo/medieval-trade.ftl`

## Station Setup
`MedievalStandardTerritory` имеет:
- `StationBankAccount` — баланс станции
- `StationCargoOrderDatabase` — `MedievalTrade` в orders, `MedievalMarket` в markets
- `MedievalTradeStation` — маркер, добавляет `TradeStationComponent` на гриды при старте

## Сущности
| ID | Назначение |
|---|---|
| `MedievalTradeConsole` | Заказ товаров (CargoOrderConsole) |
| `MedievalTradePalletBuy` | Палета приёма (Buy) |
| `MedievalTradePalletSell` | Палета продажи (Sell) |
| `MedievalTradePalletConsole` | Консоль продажи (CargoPalletConsole) |

## Adding New Products
Добавить `cargoProduct` в `medieval_trade.yml` с `group: MedievalMarket` и добавить loc-строки.
