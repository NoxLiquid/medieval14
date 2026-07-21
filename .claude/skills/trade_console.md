# Trade Console (Trade sign)

## Overview
A medieval trade system — signs for ordering and selling goods.
Uses the existing `CargoOrderConsole` + `CargoPallet` + `CargoPalletConsole` system without custom C# (except the TradeStation marker).

## Architecture

### Buying (ordering goods)
- `MedievalTradeConsole` — a sign with `CargoOrderConsole` for placing orders
- `MedievalTradePalletBuy` — a pallet with `CargoPallet (palletType: buy)` — ordered goods spawn here
- Delivery via `TryFulfillOrder()` — the system finds grids with `TradeStationComponent`, finds free Buy pallets and spawns goods on them

### Selling
- `MedievalTradePalletSell` — a pallet with `CargoPallet (palletType: sell)` — place goods here to sell
- `MedievalTradePalletConsole` — a sign with `CargoPalletConsole` — appraisal and selling of goods on Sell pallets **on the same grid**

### TradeStation on grids
- `MedievalTradeStationComponent` — a marker on the station (`Content.Shared/_Respiral/Trade/`)
- `MedievalTradeStationSystem` — on `StationPostInitEvent` adds `TradeStationComponent` to all station grids (`Content.Server/_Respiral/Trade/`)
- Without `TradeStationComponent` on a grid, `TryFulfillOrder()` won't find pallets and goods won't be delivered

## Key Design Decisions
- **DeviceLink/telepad is NOT used** — delivery is via Trade Station pallets (TryFulfillOrder), not via teleportation
- No power required: `StaticPowerSystem.IsPowered()` returns `true` if there's no `ApcPowerReceiver`
- A separate `CargoMarket` (`MedievalMarket`) and `CargoAccount` (`MedievalTrade`)

## Delivery mechanism (how it works in code)
1. The player approves an order via `MedievalTradeConsole`
2. `FulfillCargoOrderEvent` is raised
3. The telepad doesn't handle it (no DeviceLink) → fallback to `TryFulfillOrder()`
4. `GetTradeStations()` finds station grids with `TradeStationComponent`
5. `GetCargoPallets(grid, Buy)` finds Buy pallets
6. `GetFreeCargoPallets()` filters the unoccupied ones
7. `FulfillOrder()` spawns goods on the free pallets

## File Locations
- Entities: `Resources/Prototypes/_Respiral/Entities/Structures/trade_console.yml`
- Market + goods: `Resources/Prototypes/_Respiral/Catalog/medieval_trade.yml`
- Station: `Resources/Prototypes/_Respiral/Stations/base.yml`
- Marker: `Content.Server/_Respiral/Trade/Components/MedievalTradeStationComponent.cs`
- System: `Content.Server/_Respiral/Trade/MedievalTradeStationSystem.cs`
- Pricing: `Content.Server/_Respiral/Pricing/MedievalPhysicalPricedComponent.cs`
- Localization RU: `Resources/Locale/ru-RU/_Respiral/cargo/medieval-trade.ftl`
- Localization EN: `Resources/Locale/en-US/_Respiral/cargo/medieval-trade.ftl`

## Station Setup
`MedievalStandardTerritory` has:
- `StationBankAccount` — the station balance
- `StationCargoOrderDatabase` — `MedievalTrade` in orders, `MedievalMarket` in markets
- `MedievalTradeStation` — a marker, adds `TradeStationComponent` to grids at start

## Entities
| ID | Purpose |
|---|---|
| `MedievalTradeConsole` | Ordering goods (CargoOrderConsole) |
| `MedievalTradePalletBuy` | Receiving pallet (Buy) |
| `MedievalTradePalletSell` | Selling pallet (Sell) |
| `MedievalTradePalletConsole` | Selling console (CargoPalletConsole) |

## Adding New Products
Add a `cargoProduct` to `medieval_trade.yml` with `group: MedievalMarket` and add loc strings (ru-RU + en-US).

### Pricing
- Sell price = material_value × 1.2 (MedievalPhysicalPriced 20% markup)
- material_value = sum of (material_price_per_unit × quantity) from PhysicalComposition
- Buy price (cost in cargoProduct) = 2 × sell price (a temporary rule until dynamic markets)
- Formula: cost = round(material_value × 2.4)

### Categories
- `medieval-trade-category-materials` — raw materials (ore, logs)
- `medieval-trade-category-processed` — processed materials (boards, ingots, cloth, leather, glass)
- `medieval-trade-category-parts` — machine parts (MachineBoard)
- `medieval-trade-category-seeds` — seeds
