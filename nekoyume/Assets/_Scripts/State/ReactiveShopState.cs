using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Lib9c.Model.Order;
using Libplanet.Assets;
using MarketService.Response;
using Nekoyume.EnumType;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;
using UniRx;
using UnityEngine;

namespace Nekoyume.State
{
    public static class ReactiveShopState
    {
        public static ReactiveProperty<List<ItemProductResponseModel>> BuyItemProducts { get; } =
            new();

        public static ReactiveProperty<List<ItemProductResponseModel>> SellItemProducts { get; } =
            new();

        public static ReactiveProperty<List<FungibleAssetValueProductResponseModel>>
            BuyFungibleAssetProducts { get; } = new();

        public static ReactiveProperty<List<FungibleAssetValueProductResponseModel>>
            SellFungibleAssetProducts { get; } = new();

        private static readonly Dictionary<MarketOrderType,
                Dictionary<ItemSubType, List<ItemProductResponseModel>>>
            CachedBuyItemProducts = new()
            {
                {
                    MarketOrderType.cp,
                    new Dictionary<ItemSubType, List<ItemProductResponseModel>>()
                },
                {
                    MarketOrderType.cp_desc,
                    new Dictionary<ItemSubType, List<ItemProductResponseModel>>()
                },
                {
                    MarketOrderType.price,
                    new Dictionary<ItemSubType, List<ItemProductResponseModel>>()
                },
                {
                    MarketOrderType.price_desc,
                    new Dictionary<ItemSubType, List<ItemProductResponseModel>>()
                },
                {
                    MarketOrderType.grade,
                    new Dictionary<ItemSubType, List<ItemProductResponseModel>>()
                },
                {
                    MarketOrderType.grade_desc,
                    new Dictionary<ItemSubType, List<ItemProductResponseModel>>()
                },
                {
                    MarketOrderType.crystal_per_price,
                    new Dictionary<ItemSubType, List<ItemProductResponseModel>>()
                },
                {
                    MarketOrderType.crystal_per_price_desc,
                    new Dictionary<ItemSubType, List<ItemProductResponseModel>>()
                },
            };

        private static readonly List<ItemProductResponseModel> CachedSellItemProducts = new();

        private static readonly Dictionary<string, List<FungibleAssetValueProductResponseModel>>
            CachedBuyFungibleAssetProducts = new();

        private static readonly List<FungibleAssetValueProductResponseModel>
            CachedSellFungibleAssetProducts = new();

        private static readonly Dictionary<MarketOrderType, Dictionary<ItemSubType, bool>>
            BuyProductMaxChecker = new()
            {
                { MarketOrderType.cp, new Dictionary<ItemSubType, bool>() },
                { MarketOrderType.cp_desc, new Dictionary<ItemSubType, bool>() },
                { MarketOrderType.price, new Dictionary<ItemSubType, bool>() },
                { MarketOrderType.price_desc, new Dictionary<ItemSubType, bool>() },
                { MarketOrderType.grade, new Dictionary<ItemSubType, bool>() },
                { MarketOrderType.grade_desc, new Dictionary<ItemSubType, bool>() },
                { MarketOrderType.crystal_per_price, new Dictionary<ItemSubType, bool>() },
                { MarketOrderType.crystal_per_price_desc, new Dictionary<ItemSubType, bool>() },
            };

        private static readonly bool FavMaxChecker = false;

        private static List<Guid> PurchasedProductIds = new();

        public static void ClearCache()
        {
            BuyItemProducts.Value = new List<ItemProductResponseModel>();
            SellItemProducts.Value = new List<ItemProductResponseModel>();
            BuyFungibleAssetProducts.Value = new List<FungibleAssetValueProductResponseModel>();
            SellFungibleAssetProducts.Value = new List<FungibleAssetValueProductResponseModel>();

            CachedSellFungibleAssetProducts.Clear();

            foreach (var v in CachedBuyFungibleAssetProducts.Values)
            {
                v.Clear();
            }

            foreach (var v in CachedBuyItemProducts.Values)
            {
                v.Clear();
            }

            foreach (var v in BuyProductMaxChecker.Values)
            {
                v.Clear();
            }
        }

        public static async Task RequestBuyProductsAsync(
            ItemSubType itemSubType,
            MarketOrderType orderType,
            int limit)
        {
            if (Game.Game.instance.MarketServiceClient is null)
            {
                return;
            }

            if (!BuyProductMaxChecker[orderType].ContainsKey(itemSubType))
            {
                BuyProductMaxChecker[orderType].Add(itemSubType, false);
            }

            if (BuyProductMaxChecker[orderType][itemSubType])
            {
                return;
            }

            if (!CachedBuyItemProducts[orderType].ContainsKey(itemSubType))
            {
                CachedBuyItemProducts[orderType]
                    .Add(itemSubType, new List<ItemProductResponseModel>());
            }

            var offset = CachedBuyItemProducts[orderType][itemSubType].Count;
            var (products, totalCount) = await Game.Game.instance.MarketServiceClient.GetBuyProducts(itemSubType, offset, limit, orderType);
            Debug.Log($"[RequestBuyProductsAsync] : {itemSubType} / {orderType} / {offset} / {limit} / MAX:{totalCount}");

            var productModels = CachedBuyItemProducts[orderType][itemSubType];
            foreach (var product in products)
            {
                if (productModels.All(x => x.ProductId != product.ProductId))
                {
                    CachedBuyItemProducts[orderType][itemSubType].Add(product);
                }
            }

            var count = CachedBuyItemProducts[orderType][itemSubType].Count;
            BuyProductMaxChecker[orderType][itemSubType] = count == totalCount;

            SetBuyProducts(orderType);
        }

        public static async Task RequestBuyFungibleAssetsAsync(
            ItemSubType itemSubType,
            MarketOrderType orderType,
            int limit)
        {
            if (FavMaxChecker)
            {
                return;
            }

            var sum = 0;
            await foreach (var fav in CachedBuyFungibleAssetProducts.Values)
            {
                sum += fav.Count;
            }

            var offset = CachedBuyFungibleAssetProducts.Count;
            // await foreach (var ticker in Util.GetTickers())
            // {
            //     var (fungibleAssets, totalCount) =
            //         await Game.Game.instance.MarketServiceClient.GetBuyFungibleAssetProducts(ticker, offset, limit);
            //     await foreach (var model in fungibleAssets)
            //     {
            //         if (!CachedBuyFungibleAssetProducts.Exists(x => x.ProductId == model.ProductId))
            //         {
            //             CachedBuyFungibleAssetProducts.Add(model);
            //         }
            //     }
            // }
            //
            // FavMaxChecker = totalCount == CachedBuyFungibleAssetProducts.Count;

            SetBuyFungibleAssets();
        }

        public static async Task RequestSellProductsAsync()
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var (fungibleAssets, items) =
                await Game.Game.instance.MarketServiceClient.GetProducts(avatarAddress);
            CachedSellItemProducts.Clear();
            CachedSellItemProducts.AddRange(items);

            CachedSellFungibleAssetProducts.Clear();
            CachedSellFungibleAssetProducts.AddRange(fungibleAssets);
            SetSellProducts();
        }

        public static void SetBuyProducts(MarketOrderType marketOrderType)
        {
            var products = new List<ItemProductResponseModel>();
            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
            foreach (var models in CachedBuyItemProducts[marketOrderType].Values)
            {
                products.AddRange(models.Where(x => x.RegisteredBlockIndex + Order.ExpirationInterval - currentBlockIndex > 0));
            }

            var agentAddress = States.Instance.AgentState.address;
            var buyProducts = products
                .Where(x => !x.SellerAgentAddress.Equals(agentAddress))
                .Where(x => !PurchasedProductIds.Contains(x.ProductId))
                .ToList();
            BuyItemProducts.Value = buyProducts;
        }

        public static void SetBuyFungibleAssets()
        {
            var fav = new List<FungibleAssetValueProductResponseModel>();
            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
            foreach (var models in CachedBuyFungibleAssetProducts.Values)
            {
                fav.AddRange(models.Where(x => x.RegisteredBlockIndex + Order.ExpirationInterval - currentBlockIndex > 0));
            }

            var agentAddress = States.Instance.AgentState.address;
            var favProducts = fav
                .Where(x => !x.SellerAgentAddress.Equals(agentAddress))
                .Where(x => !PurchasedProductIds.Contains(x.ProductId))
                .ToList();
            BuyFungibleAssetProducts.Value = favProducts;
        }

        public static void SetSellProducts()
        {
            SellItemProducts.Value = CachedSellItemProducts;
            SellFungibleAssetProducts.Value = CachedSellFungibleAssetProducts;
        }

        public static void UpdatePurchaseProductIds(IEnumerable<Guid> ids)
        {
            foreach (var guid in ids.Where(guid => !PurchasedProductIds.Contains(guid)))
            {
                PurchasedProductIds.Add(guid);
            }
        }

        public static void RemoveSellProduct(Guid productId)
        {
            var itemProduct = SellItemProducts.Value.FirstOrDefault(x =>
                x.ProductId.Equals(productId));
            if (itemProduct is not null)
            {
                SellItemProducts.Value.Remove(itemProduct);
                SellItemProducts.SetValueAndForceNotify(SellItemProducts.Value);
            }

            var fungibleAssetProduct = SellFungibleAssetProducts.Value.FirstOrDefault(x =>
                x.ProductId.Equals(productId));
            if (fungibleAssetProduct is not null)
            {
                SellFungibleAssetProducts.Value.Remove(fungibleAssetProduct);
                SellFungibleAssetProducts.SetValueAndForceNotify(SellFungibleAssetProducts.Value);
            }
        }

        public static ItemProductResponseModel GetSellItemProduct(Guid productId)
        {
            return SellItemProducts.Value.FirstOrDefault(x => x.ProductId == productId);
        }

        public static FungibleAssetValueProductResponseModel GetSellFungibleAssetProduct(
            Guid productId)
        {
            return SellFungibleAssetProducts.Value.FirstOrDefault(x => x.ProductId == productId);
        }

        public static bool TryGetItemBase(ItemProductResponseModel product, out ItemBase itemBase)
        {
            var itemRow = Game.Game.instance.TableSheets.ItemSheet[product.ItemId];
            var id = product.TradableId;
            var requiredBlockIndex = product.RegisteredBlockIndex + Order.ExpirationInterval;
            var madeWithMimisbrunnrRecipe = false;
            ITradableItem tradableItem = null;
            switch (itemRow.ItemSubType)
            {
                // Consumable
                case ItemSubType.Food:
                    tradableItem = new Consumable((ConsumableItemSheet.Row)itemRow, id,
                        requiredBlockIndex);
                    break;
                // Equipment
                case ItemSubType.Weapon:
                    tradableItem = new Weapon((EquipmentItemSheet.Row)itemRow, id,
                        requiredBlockIndex, madeWithMimisbrunnrRecipe);
                    break;
                case ItemSubType.Armor:
                    tradableItem = new Armor((EquipmentItemSheet.Row)itemRow, id,
                        requiredBlockIndex, madeWithMimisbrunnrRecipe);
                    break;
                case ItemSubType.Belt:
                    tradableItem = new Belt((EquipmentItemSheet.Row)itemRow, id,
                        requiredBlockIndex, madeWithMimisbrunnrRecipe);
                    break;
                case ItemSubType.Necklace:
                    tradableItem = new Necklace((EquipmentItemSheet.Row)itemRow, id,
                        requiredBlockIndex, madeWithMimisbrunnrRecipe);
                    break;
                case ItemSubType.Ring:
                    tradableItem = new Ring((EquipmentItemSheet.Row)itemRow, id,
                        requiredBlockIndex, madeWithMimisbrunnrRecipe);
                    break;
                case ItemSubType.ApStone:
                case ItemSubType.Hourglass:
                    tradableItem = new TradableMaterial((MaterialItemSheet.Row)itemRow);
                    break;
                case ItemSubType.EarCostume:
                case ItemSubType.EyeCostume:
                case ItemSubType.FullCostume:
                case ItemSubType.HairCostume:
                case ItemSubType.TailCostume:
                case ItemSubType.Title:
                    tradableItem = new Costume((CostumeItemSheet.Row)itemRow, id);
                    break;
            }

            if (tradableItem is ItemUsable itemUsable)
            {
                foreach (var skillModel in product.SkillModels)
                {
                    var skillRow =
                        Game.Game.instance.TableSheets.SkillSheet[skillModel.SkillId];
                    var skill = SkillFactory.Get(skillRow, skillModel.Power, skillModel.Chance);
                    itemUsable.Skills.Add(skill);
                }

                foreach (var statModel in product.StatModels)
                {
                    if (statModel.Additional)
                    {
                        itemUsable.StatsMap.AddStatAdditionalValue(statModel.Type,
                            statModel.Value);
                    }
                    else
                    {
                        var current = itemUsable.StatsMap.GetBaseStats(true)
                            .First(r => r.statType == statModel.Type).baseValue;
                        itemUsable.StatsMap.AddStatValue(statModel.Type,
                            statModel.Value - current);
                    }
                }
            }

            if (tradableItem is Equipment equipment)
            {
                equipment.level = product.Level;
            }

            itemBase = (ItemBase)tradableItem;
            return itemBase is not null;
        }

        public static int GetCachedBuyItemCount(MarketOrderType orderType, ItemSubType itemSubType)
        {
            if (!CachedBuyItemProducts.ContainsKey(orderType))
                return 0;

            if (!CachedBuyItemProducts[orderType].ContainsKey(itemSubType))
                return 0;

            var curBlockIndex = Game.Game.instance.Agent.BlockIndex;
            return CachedBuyItemProducts[orderType][itemSubType]
                .Count(x => x.RegisteredBlockIndex + Order.ExpirationInterval - curBlockIndex > 0);
        }

        public static int GetCachedBuyItemCount(MarketOrderType orderType, ItemSubTypeFilter filter)
        {
            switch (filter)
            {
                case ItemSubTypeFilter.RuneStone:
                case ItemSubTypeFilter.PetSoulStone:
                    // CachedBuyFungibleAssetProducts.Where(x=> x.Ticker)
                    return 0;

                default:
                    if (!CachedBuyItemProducts.ContainsKey(orderType))
                    {
                        return 0;
                    }

                    var itemSubType = filter.ToItemSubType();
                    if (!CachedBuyItemProducts[orderType].ContainsKey(itemSubType))
                    {
                        return 0;
                    }


                    var curBlockIndex = Game.Game.instance.Agent.BlockIndex;
                    var items = CachedBuyItemProducts[orderType][itemSubType]
                        .Where(x => x.RegisteredBlockIndex + Order.ExpirationInterval - curBlockIndex > 0)
                        .ToList();

                    if (itemSubType == ItemSubType.Food)
                    {
                        var foods = new Dictionary<ItemSubTypeFilter, int>();
                        foreach (var item in items)
                        {
                            var filters = ItemSubTypeFilterExtension.GetItemSubTypeFilter(item.ItemId);
                            foreach (var itemSubTypeFilter in filters)
                            {
                                if (!foods.ContainsKey(itemSubTypeFilter))
                                {
                                    foods.Add(itemSubTypeFilter, 0);
                                }

                                foods[itemSubTypeFilter]++;
                            }
                        }

                        return foods.ContainsKey(filter) ? foods[filter] : 0;
                    }

                    return items.Count;
            }
        }
    }
}
