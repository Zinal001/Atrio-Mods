using Isto.Atrio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.UI;

namespace VoidChest
{
    static class Patches
    {
        private static System.Reflection.MethodInfo PlayerPlaceItemBaseState_SetFlowState_Method = HarmonyLib.AccessTools.Method(typeof(PlayerPlaceItemBaseState), "SetFlowState");
        private static System.Reflection.MethodInfo PlayerPlaceItemBaseState_GetStartingPositionForDrop_Method = HarmonyLib.AccessTools.Method(typeof(PlayerPlaceItemBaseState), "GetStartingPositionForDrop");
        private static System.Reflection.MethodInfo PlayerPlaceItemBaseState_CompleteItemInitialization_Method = HarmonyLib.AccessTools.Method(typeof(PlayerPlaceItemBaseState), "CompleteItemInitialization");
        private static System.Reflection.MethodInfo PlayerPlaceItemState_StartNewDrop_Method = HarmonyLib.AccessTools.Method(typeof(PlayerPlaceItemState), "StartNewDrop");
        private static System.Reflection.FieldInfo PlayerPlaceItemBaseState__currentFlowState_Field = HarmonyLib.AccessTools.Field(typeof(PlayerPlaceItemBaseState), "_currentFlowState");
        private static System.Reflection.FieldInfo PlayerPlaceItemState__placementControl_Field = HarmonyLib.AccessTools.Field(typeof(PlayerPlaceItemState), "_placementControl");
        private static System.Reflection.PropertyInfo ItemPlacementController_CurrentItem_Property = HarmonyLib.AccessTools.Property(typeof(ItemPlacementController), nameof(ItemPlacementController.CurrentItem));

        private static GameObject _VoidChestObj = null;
        private static Zenject.DiContainer _DiContainer = null;

        private static Texture2D _VoidChestItemTexture;
        private static Sprite _VoidChestItemSprite;

        private static void CreateVoidChestItem(MasterItemList itemList, AdvancedItem scrapChestItem)
        {
            //Create a new item based on the item for ScrapChest
            AdvancedItem voidChestItem = UnityEngine.Object.Instantiate(scrapChestItem);

            //Set new information (These will later be used by CModLib.Loc)
            voidChestItem.itemNameLoc = (I2.Loc.LocalizedString)"VOIDCHEST_NAME";
            voidChestItem.itemID = "VoidChest";
            voidChestItem.flavorTextLoc = (I2.Loc.LocalizedString)"VOIDCHEST_DESCRIPTION";
            voidChestItem.interactToolTipTextLoc = (I2.Loc.LocalizedString)"VOIDCHEST_TOOLTIP";

            if (_VoidChestItemSprite == null)
            {
                _VoidChestItemTexture = new Texture2D(voidChestItem.icon.texture.width, voidChestItem.icon.texture.height);
                if(_VoidChestItemTexture.LoadImage(System.IO.File.ReadAllBytes(System.IO.Path.Combine(Glob.PluginLocation, "Resources", "VoidChest_Icon.png"))))
                    _VoidChestItemSprite = Sprite.Create(_VoidChestItemTexture, voidChestItem.icon.rect, voidChestItem.icon.pivot);
            }

            if(_VoidChestItemSprite != null)
                voidChestItem.icon = _VoidChestItemSprite;


            //Create a new recipe based on the Scrap Chest recipe.
            Recipe scrapRecipe = scrapChestItem.GetRecipe();

            Recipe newRecipe = ScriptableObject.CreateInstance<Recipe>();
            newRecipe.inputs = scrapRecipe.inputs;
            newRecipe.outputs = new List<ItemPile>() {
                new ItemPile(voidChestItem, 1)
            };
            newRecipe.cookTime = scrapRecipe.cookTime;
            voidChestItem.SetRecipe(newRecipe);

            //Remove the ScrapChest MonoBehaviour and add the VoidChest behaviour instead!
            ScrapChest sc = voidChestItem.prefab.GetComponent<ScrapChest>();
            if (sc != null)
                UnityEngine.Object.DestroyImmediate(sc);

            if (voidChestItem.prefab.GetComponent<VoidChest>() == null)
            {
                VoidChest vc = voidChestItem.prefab.AddComponent<VoidChest>();
                voidChestItem.prefab.name = "VoidChest";
                vc.itemPile = new ItemPile(voidChestItem, 1);
            }

            //Set the inherited ItemController's item to the VoidChestItem, instead of the ScrapChestItem.
            ItemController ic = voidChestItem.coreItemPrefab?.GetComponent<ItemController>();
            if (ic != null)
            {
                ic.SetItem(voidChestItem, 1);
                ic.SetPropertyBlockSet(voidChestItem.emissionSprite, voidChestItem.emissionFactor, voidChestItem.emissionTint);
            }

            //Add the item to the master list
            itemList.items.Add(voidChestItem);


            VoidChest.Item = voidChestItem;
            VoidChest.ScrapChestItem = scrapChestItem;

            if (_DiContainer != null)
            {
                //Since this item was created AFTER the auto-injection of fields, queue it for injection manually.
                _DiContainer.QueueForInject(voidChestItem);
            }
        }


        private static VoidChest OverwriteScrapChest(ref GameObject gameObject)
        {
            if(gameObject.GetComponent<VoidChest>() == null)
            {
                ScrapChest sc = gameObject.GetComponent<ScrapChest>();
                VoidChest vc = gameObject.AddComponent<VoidChest>();
                if (sc != null)
                {
                    vc.outlines = new List<GameObject>(sc.outlines);
                    vc.pullsFromNeighbors = sc.pullsFromNeighbors;
                    vc.rigidInventory = sc.rigidInventory;

                    UnityEngine.Object.DestroyImmediate(sc);
                }

                _DiContainer.Inject(vc);
                return vc;
            }

            return null;
        }


        [HarmonyLib.HarmonyPatch(typeof(ScrapChest), "SetGridSpace")]
        [HarmonyLib.HarmonyPostfix()]
        private static void ScrapChest_SetGridSpace_Postfix(ScrapChest __instance, AutomationGridSpace space)
        {
            if(Glob.VoidChestSaveData.Contains(__instance.transform.position) && (!Glob.VoidChestsLoaded.ContainsKey(__instance.transform.position)))
            {
                GameObject go = __instance.gameObject;
                VoidChest vc = OverwriteScrapChest(ref go);

                if (vc != null)
                    vc.SetGridSpace(space);

                Glob.VoidChestsLoaded[go.transform.position] = true;
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(EssentialsSceneInstaller), "BindMasterItemList")]
        [HarmonyLib.HarmonyPostfix()]
        private static void EssentialsSceneInstaller_BindMasterItemList_Postfix(Zenject.DiContainer ____container)
        {
            _DiContainer = ____container;
        }


        [HarmonyLib.HarmonyPatch(typeof(AdvancedItem), "InitializeNewItem")]
        [HarmonyLib.HarmonyPrefix()]
        private static void AdvancedItem_InitializeNewItem_Prefix(GameObject newItem)
        {
            if (newItem == _VoidChestObj)
            {
                ScrapChest sc = newItem.GetComponent<ScrapChest>();

                VoidChest vc = newItem.AddComponent<VoidChest>();
                if (sc != null)
                {
                    vc.outlines = new List<GameObject>(sc.outlines);
                    vc.pullsFromNeighbors = sc.pullsFromNeighbors;
                    vc.rigidInventory = sc.rigidInventory;

                    UnityEngine.Object.DestroyImmediate(sc);
                }

                _DiContainer.Inject(vc);

                _VoidChestObj = null;
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(UICraftingTab), "Setup")]
        [HarmonyLib.HarmonyPostfix()]
        private static void UICraftingTab_Setup_Prefix(UICraftingTab __instance, CraftingCategory category, PlayerProgress ____playerProgress)
        {
            if(category.titleKey.mTerm == "UI/Crafting/AutomationCategory")
            {
                if (!category.itemList[1].list.Contains(VoidChest.Item))
                {
                    category.itemList[1].list.Add(VoidChest.Item);
                }

                if (____playerProgress.IsItemUnlocked(VoidChest.ScrapChestItem) && !____playerProgress.IsItemUnlocked(VoidChest.Item))
                    ____playerProgress.UnlockItem(VoidChest.Item, false);
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(UICraftingTabPage), "SetUpGrid")]
        [HarmonyLib.HarmonyPrefix()]
        private static void UICraftingTabPage_SetUpGrid_Prefix(UICraftingTab uiCraftingTab, VerticalLayoutGroup ____vertLayout)
        {
            if(uiCraftingTab != null && uiCraftingTab.category != null && uiCraftingTab.category.titleKey.mTerm == "UI/Crafting/AutomationCategory" && ____vertLayout != null && ____vertLayout.transform.childCount > 1)
            {
                HorizontalLayoutGroup firstGroup = ____vertLayout.transform.GetChild(1).GetComponent<HorizontalLayoutGroup>();

                if(firstGroup != null && firstGroup.transform.childCount > 0)
                {
                    GameObject firstSlot = firstGroup.transform.GetChild(0).gameObject;
                    if(firstSlot != null)
                    {
                        GameObject newSlot = UnityEngine.Object.Instantiate(firstSlot, firstGroup.transform);
                        if (_DiContainer != null)
                            _DiContainer.Inject(newSlot.GetComponent<UICraftingSlot>());
                    }
                }
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(PlayerPlaceItemBaseState), "CompleteItemInitialization")]
        [HarmonyLib.HarmonyPrefix()]
        private static void PlayerPlaceItemBaseState_CompleteItemInitialization_Prefix(PlayerPlaceItemBaseState __instance, GameObject createdObject)
        {
            if(createdObject.name == "VoidChest")
            {
                if (createdObject.TryGetComponent(out ItemPlacementController itemPlacementController))
                {
                    ItemPlacementController_CurrentItem_Property.SetValue(itemPlacementController, new ItemPile(VoidChest.Item, 1));
                }
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(PlayerPlaceItemBaseState), "SetupItemDrop")]
        [HarmonyLib.HarmonyPrefix()]
        private static bool PlayerPlaceItemBaseState_SetupItemDrop_Prefix(PlayerPlaceItemBaseState __instance, CoreItem dropItem, PlayerController ____playerController, ref AdvancedItem ____itemToDrop, ref Transform ____dropItemTrans,
            PooledItemFactory ____pooledItemFactory)
        {

            if(__instance is PlayerPlaceItemState playerPlaceItemState)
            {
                if(playerPlaceItemState.IsRunning)
                {
                    ItemPlacementController placementControl = (ItemPlacementController)PlayerPlaceItemState__placementControl_Field.GetValue(playerPlaceItemState);
                    if (placementControl != null)
                        placementControl.CancelDrop(false);

                    if(____itemToDrop != dropItem)
                    {
                        ____itemToDrop = dropItem as AdvancedItem;
                        PlayerPlaceItemState_StartNewDrop_Method.Invoke(playerPlaceItemState, null);
                        return false;
                    }
                }
            }

            int currentFlowState = (int)PlayerPlaceItemBaseState__currentFlowState_Field.GetValue(__instance);

            switch (currentFlowState)
            {
                case 0x00000000:
                    PlayerPlaceItemBaseState_SetFlowState_Method.Invoke(__instance, new object[] { Enum.ToObject(PlayerPlaceItemBaseState__currentFlowState_Field.FieldType, 0x00000001) });
                    break;
                case 0x00000001:
                    break;
                case 0x00000002:
                case 0x00000004:
                    PlayerPlaceItemBaseState_SetFlowState_Method.Invoke(__instance, new object[] { Enum.ToObject(PlayerPlaceItemBaseState__currentFlowState_Field.FieldType, 0x00000003) });
                    break;
                default:
                    Debug.LogError(string.Format("Unexpected PlacementFlowState {0} during SetupItemDrop", currentFlowState));
                    ____playerController.ChangeToMoveState();
                    break;
            }

            ____itemToDrop = dropItem as AdvancedItem;
            ____dropItemTrans = null;

            Vector3 dropPosition = (Vector3)PlayerPlaceItemBaseState_GetStartingPositionForDrop_Method.Invoke(__instance, null);
            GameObject pooledItem = ____pooledItemFactory.GetPooledItem(____itemToDrop);
            if (pooledItem != null)
            {
                if (dropItem.itemID == "VoidChest")
                {
                    _VoidChestObj = pooledItem;
                    _VoidChestObj.name = "VoidChest";
                }
                AdvancedItem.InitializeNewItem(pooledItem, false);
                PlayerPlaceItemBaseState_CompleteItemInitialization_Method.Invoke(__instance, new object[] { pooledItem });
                return false;
            }

            if (!String.IsNullOrEmpty(dropItem.addressableAsset.AssetGUID))
            {
                Action<AsyncOperationHandle<GameObject>> action = null;
                UnityEngine.AddressableAssets.Addressables.LoadResourceLocationsAsync(dropItem.addressableAsset, null).Completed += delegate (AsyncOperationHandle<IList<IResourceLocation>> op)
                {
                    if (op.Result != null && op.Result.Count > 0)
                    {
                        AsyncOperationHandle<GameObject> asyncOperationHandle = dropItem.addressableAsset.InstantiateAsync(dropPosition + Vector3.up * 1000f, Quaternion.identity, null);
                        Action<AsyncOperationHandle<GameObject>> value;
                        if ((value = action) == null)
                        {
                            value = (action = delegate (AsyncOperationHandle<GameObject> op) {
                                if (op.Status == AsyncOperationStatus.Succeeded)
                                {
                                    GameObject result = op.Result;
                                    if (dropItem.itemID == "VoidChest")
                                    {
                                        _VoidChestObj = result;
                                        _VoidChestObj.name = "VoidChest";
                                    }
                                    AdvancedItem.InitializeNewItem(result, false);
                                    PlayerPlaceItemBaseState_CompleteItemInitialization_Method.Invoke(__instance, new object[] { result });
                                }
                            });
                        }
                        asyncOperationHandle.Completed += value;
                        return;
                    }

                    Debug.LogError("Cannot find item " + dropItem.itemID + " with key " + dropItem.addressableAsset.RuntimeKey.ToString());
                };

                return false;
            }

            Debug.LogWarning("No valid AddressableAsset GUID for " + dropItem.itemName + ". Cannot drop");
            return true;
        }

        [HarmonyLib.HarmonyPatch(typeof(MasterItemList), "Init")]
        [HarmonyLib.HarmonyPrefix()]
        private static void MasterItemList_Init_Prefix(MasterItemList __instance)
        {
            foreach (Item item in __instance.items.ToArray())
            {
                if (item != null && ("ScrapChest".Equals(item.itemName, StringComparison.CurrentCultureIgnoreCase) || "ScrapChest".Equals(item.itemID, StringComparison.CurrentCultureIgnoreCase)) && item is AdvancedItem aItem)
                {
                    CreateVoidChestItem(__instance, aItem);
                }
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(GameState), "LoadGameStateData")]
        [HarmonyLib.HarmonyPrefix()]
        private static void GameState_LoadGameStateData_Prefix(int saveSlot)
        {
            Glob.VoidChestsLoaded.Clear();
        }

        [HarmonyLib.HarmonyPatch(typeof(GameState), "LoadGameStateData")]
        [HarmonyLib.HarmonyPostfix()]
        private static void GameState_LoadGameStateData_Postfix(int saveSlot)
        {
            Glob.SaveManager.LoadAll(saveSlot);
        }

        [HarmonyLib.HarmonyPatch(typeof(GameState), "SaveGameStateData")]
        [HarmonyLib.HarmonyPostfix()]
        private static void GameState_SaveGameStateData_Postfix(int ____saveSlot)
        {
            Glob.SaveManager.SaveAll(____saveSlot);
        }

        [HarmonyLib.HarmonyPatch(typeof(GameState), "SendLoadCompleteEvent")]
        [HarmonyLib.HarmonyPostfix()]
        private static void GameState_SendLoadCompleteEvent_Postfix()
        {
            CModLib.Loc.Init();
        }
    }
}
