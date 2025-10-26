using BepInEx;
using HarmonyLib;
using Microsoft.Cci;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EquippableHiddenWorldSkins
{
    [BepInPlugin("com.aidanamite.EquippableHiddenWorldSkins", "Equippable HW Skins", VERSION)]
    public class Main : BaseUnityPlugin
    {
        public const string VERSION = "1.0.1";

        public static UserItemData HiddenWorldSkin;
        public void Awake()
        {
            ItemData item;
            HiddenWorldSkin = new UserItemData()
            {
                ItemTier = null,
                CreatedDate = new DateTime(638428661429519390L, DateTimeKind.Utc),
                Item = item = new ItemData()
                {
                    AllowStacking = true,
                    AssetName = null,
                    Attribute = new[]
                    {
                        new ItemAttribute()
                        {
                            Key = "PetTypeID",
                            Value = "-1"
                        },
                        new ItemAttribute()
                        {
                            Key = "PetStage",
                            Value = nameof(RaisedPetStage.BABY)
                        }
                    },
                    Availability = null,
                    BluePrint = null,
                    CashCost = 0,
                    Category = new[]
                    {
                        new ItemDataCategory() { CategoryId = Category.DragonSkin }
                    },
                    Cost = 0,
                    CreativePoints = 0,
                    Description = "HW skin",
                    Geometry2 = null,
                    IconName = "RS_DATA/collectdwicons/icodwdragonsd3expansion.png",
                    InventoryMax = 1,
                    IsNew = false,
                    ItemID = 2100010,
                    ItemName = "Hidden World Appearance",
                    ItemNamePlural = null,
                    ItemRarity = null,
                    ItemSaleConfigs = null,
                    ItemStates = new List<ItemState>(),
                    ItemStatsMap = null,
                    Locked = false,
                    MemberSaleList = null,
                    Points = null,
                    PopularRank = -1,
                    PossibleStatsMap = null,
                    RankId = null,
                    Relationship = null,
                    RewardTypeID = 0,
                    Rollover = null,
                    SaleFactor = 0,
                    SaleList = null,
                    Stackable = true,
                    Texture = null,
                    Uses = -1
                },
                ItemID = item.ItemID,
                ItemStats = null,
                ModifiedDate = new DateTime(638428661429519390L, DateTimeKind.Utc),
                Quantity = 1,
                UserItemAttributes = null,
                UserInventoryID = int.MinValue,
                Uses = item.Uses
            };
            new Harmony("com.aidanamite.EquippableHiddenWorldSkins").PatchAll();
            Logger.LogInfo("Loaded");
        }
    }


    [HarmonyPatch]
    static class Patch_InventoryCreation
    {
        static IEnumerable<MethodBase> TargetMethods() => new MethodBase[]
        {
            AccessTools.Constructor(typeof(CommonInventoryData)),
            AccessTools.Method(typeof(CommonInventoryData), "Clear"),
            AccessTools.Method(typeof(CommonInventoryData), "InitDefault")
        };
        public static void Postfix(CommonInventoryData __instance)
        {
            __instance.AddToCategories(Main.HiddenWorldSkin);
            ItemData.AddToCache(Main.HiddenWorldSkin.Item);
        }
    }

    // Prevents the skins from appearing twice in the customization UI
    [HarmonyPatch(typeof(KAUISelectDragonMenu))]
    static class Patch_InventoryOpen
    {
        static ConditionalWeakTable<KAUISelectDragonMenu, HashSet<int>> added = new ConditionalWeakTable<KAUISelectDragonMenu, HashSet<int>>();
        [HarmonyPatch("AddInvMenuItem")]
        static bool Prefix(KAUISelectDragonMenu __instance, UserItemData userItem)
        {
            if (userItem == Main.HiddenWorldSkin)
            {
                var skindata = SanctuaryManager.pCurPetInstance.GetCustomSkinData("HWGlow");
                if (!string.IsNullOrEmpty(skindata?._ResourcePath))
                {
                    userItem.Item.Attribute.First(x => x.Key == "PetTypeID").Value = __instance.mUiSelectDragon.pPetData.PetTypeID.ToString();
                    userItem.Item.AssetName = skindata._ResourcePath;
                }
            }
            if (!added.GetOrCreateValue(__instance).Add(userItem.ItemID))
                return false;
            return true;
        }
        [HarmonyPatch("FinishMenuItems")]
        static void Prefix(KAUISelectDragonMenu __instance) => added.GetOrCreateValue(__instance).Clear();
    }

    [HarmonyPatch(typeof(SanctuaryPet), "UpdateMaterials")]
    static class Patch_FixGlowWeirdness
    {
        static void Prefix(SanctuaryPet __instance)
        {
            if (__instance.pData?.Accessories == null)
                return;
            var skin = __instance.GetCustomSkinData("HWGlow");
            if (skin != null && skin._ApplyGlowParameters && !string.IsNullOrEmpty(skin._ResourcePath))
            {
                var path = skin._ResourcePath + "&";
                foreach (var acc in __instance.pData.Accessories)
                    if (!string.IsNullOrEmpty(acc?.Geometry) && acc.Geometry.StartsWith(path,StringComparison.OrdinalIgnoreCase))
                    {
                        __instance.mApplyGlowSkinParameters = true;
                        break;
                    }
            }
        }
    }
}