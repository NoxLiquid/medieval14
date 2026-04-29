# Economy System

## Overview
Medieval economy items: coins, wallet (coin bag), and gemstones as raw materials.

## Coins
- **MedievalGoldCoin** — gold coin, crafted on anvil for 10 MedievalProcessedGold
- **MedievalSilverCoin** — silver coin, crafted on anvil for 10 MedievalProcessedSilver
- Coins are **stackable** (maxCount: 10) with `layerStates` coin0–coin10 that change sprite based on stack count
- Stack types: `MedievalGoldCoin`, `MedievalSilverCoin`
- Tags: `MedievalCoin` (shared), `MedievalGoldCoin`, `MedievalSilverCoin`
- Size: Tiny

## Wallet (MedievalWallet)
- Leather coin bag using `ItemMapper` to change sprite based on coin count (wallet-0 through wallet-3)
- Storage whitelist: only items with `MedievalCoin` tag
- Grid: 5x2, maxItemSize: Tiny
- Sprites in `wallet.rsi`

## Gemstone Materials
Four gem types as raw materials (maxCount: 1 stacks):
- **MedievalRawEmerald** → MedievalEmeraldGem (emerald1–4 random sprites)
- **MedievalRawSapphire** → MedievalSapphireGem (sapphire1–3 random sprites)
- **MedievalRawRuby** → MedievalRubyGem (ruby1–3 random sprites)
- **MedievalRawTopaz** → MedievalTopazGem (topaz1 sprite)
- All gems inherit from `MedievalCrystalBase` (sharp, melee weapon, damageable)
- Sprites in `jewelry.rsi`

## File Locations
- Entities: `Resources/Prototypes/_Respiral/Entities/Items/Misk/economy.yml`
- Gem entities: `Resources/Prototypes/_Respiral/Entities/Objects/RawMaterials/production.yml`
- Materials: `Resources/Prototypes/_Respiral/Entities/Objects/RawMaterials/materials.yml`
- Stacks: `Resources/Prototypes/_Respiral/Entities/Objects/RawMaterials/stack.yml`
- Recipes: `Resources/Prototypes/_Respiral/Recipe/recipe.yml` (MedievalGoldCoinRecipe, MedievalSilverCoinRecipe)
- Recipe pack: `Resources/Prototypes/_Respiral/Recipe/packs.yml` (MedievalAnvil)
- Tags: `Resources/Prototypes/_Respiral/tags.yml`
- Textures: `Resources/Textures/_Respiral/Objects/Items/Economy/`
