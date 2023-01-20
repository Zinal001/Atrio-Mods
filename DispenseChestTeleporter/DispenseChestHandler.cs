using Isto.Atrio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DispenseChestTeleporter
{
    static class DispenseChestHandler
    {
        internal static int GameSaveSlot = -1;

        private static readonly System.Reflection.MethodInfo AutomationItemDispenser_TryDropItem_Method = HarmonyLib.AccessTools.DeclaredMethod(typeof(AutomationItemDispenser), "TryDropItem");

        private static List<ChestLink> _ChestLinks = new List<ChestLink>();

        private static DispenseChest _LastChestInteracted = null;
        private static DateTime _LastChestInteractedAt = DateTime.MinValue;
        private static DispenseChest _CurrentInput = null;
        private static AutomationItemDispenser _CurrentAutomationDispenser = null;
        private static UIPlayerMessageDisplay _PlayerMessageDisplay;

        private static Dictionary<DispenseChest, bool> _ChestsLoaded = null;

        private static void SaveData(int slotNumber)
        {
            String json = Newtonsoft.Json.JsonConvert.SerializeObject(_ChestLinks, Newtonsoft.Json.Formatting.Indented);
            String filename = Path.Combine(Plugin.PluginLocation, $"{slotNumber}_ChestLinks.json");
            File.WriteAllText(filename, json);
            Plugin.PluginLogger.LogDebug($"Saved {_ChestLinks.Count} chestlinks to file: {filename}.");
        }

        private static void LoadData(int slotNumber)
        {
            String filename = Path.Combine(Plugin.PluginLocation, $"{slotNumber}_ChestLinks.json");
            if (File.Exists(filename))
            {
                String json = File.ReadAllText(filename);
                List<ChestLink> links = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ChestLink>>(json);
                if (links != null)
                {
                    _ChestLinks = links;
                    Plugin.PluginLogger.LogDebug($"Loaded {_ChestLinks.Count} chestlinks from file: {filename}.");
                }
            }

            _ChestsLoaded = new Dictionary<DispenseChest, bool>();
        }

        private static void SetupTeleport(DispenseChest outputChest, AutomationItemDispenser outputAutomationDispenser)
        {
            ChestLink link = _ChestLinks.FirstOrDefault(c => c.InputChest == _CurrentInput && c.OutputChest == outputChest);
            if (link == null)
            {
                _ChestLinks.Add(new ChestLink()
                {
                    InputPosition = _CurrentInput.transform.position,
                    OutputPosition = outputChest.transform.position,
                    InputChest = _CurrentInput,
                    InputAutomationDispenser = _CurrentAutomationDispenser,
                    OutputChest = outputChest,
                    OutputAutomationDispenser = outputAutomationDispenser
                });
            }
            else
            {
                link.OutputAutomationDispenser = outputAutomationDispenser;
                link.OutputPosition = outputChest.transform.position;
            }
        }

        #region Patches

        [HarmonyLib.HarmonyPatch(typeof(DispenseChest), "PlayerInteraction")]
        [HarmonyLib.HarmonyPrefix()]
        private static bool DispenseChest_PlayerInteraction_Prefix(DispenseChest __instance, Controls.UserActions action, AutomationItemDispenser ____systemContainer)
        {
            if (action == Controls.UserActions.Interact && Input.GetKey(KeyCode.LeftControl))
            {
                if (_LastChestInteracted == __instance && DateTime.Now.Subtract(_LastChestInteractedAt).TotalMilliseconds < 500)
                    return false;

                _LastChestInteracted = __instance;
                _LastChestInteractedAt = DateTime.Now;

                if (_CurrentInput == null)
                {
                    _CurrentInput = __instance;
                    _CurrentAutomationDispenser = ____systemContainer;
                    Plugin.PluginLogger.LogDebug($"Input specified!");

                    if (_PlayerMessageDisplay != null)
                        _PlayerMessageDisplay.DisplayMessage("Input specified successuly, select output Dispense Chest.", 6f);
                }
                else if (__instance != _CurrentInput)
                {
                    SetupTeleport(__instance, ____systemContainer);
                    _CurrentInput = null;
                    _CurrentAutomationDispenser = null;
                    Plugin.PluginLogger.LogDebug($"Output specified, link created!");

                    if (_PlayerMessageDisplay != null)
                        _PlayerMessageDisplay.DisplayMessage("Dispense Chest link successfully established!", 6f);
                }

                return false;
            }

            return true;
        }

        [HarmonyLib.HarmonyPatch(typeof(DispenseChest), "Update")]
        [HarmonyLib.HarmonyPostfix()]
        private static void DispenseChest_Update_Postfix(DispenseChest __instance, AutomationItemDispenser ____systemContainer)
        {
            if (_ChestsLoaded != null && (!_ChestsLoaded.ContainsKey(__instance) || !_ChestsLoaded[__instance]))
            {
                ChestLink link = _ChestLinks.FirstOrDefault(c => c.InputPosition.Equals(__instance.transform.position) || c.OutputPosition.Equals(__instance.transform.position));
                if (link != null)
                {
                    if (link.InputPosition.Equals(__instance.transform.position))
                    {
                        link.InputChest = __instance;
                        link.InputAutomationDispenser = ____systemContainer;
                        Plugin.PluginLogger.LogDebug($"OnDataLoadComplete: {__instance.transform.position}. IS INPUT");
                    }
                    else
                    {
                        link.OutputChest = __instance;
                        link.OutputAutomationDispenser = ____systemContainer;
                        Plugin.PluginLogger.LogDebug($"OnDataLoadComplete: {__instance.transform.position}. IS OUTPUT");
                    }
                }
                else
                    Plugin.PluginLogger.LogDebug($"OnDataLoadComplete: {__instance.transform.position}. NONE");

                _ChestsLoaded[__instance] = true;
            }
        }


        [HarmonyLib.HarmonyPatch(typeof(UIPlayerMessageDisplay), "Awake")]
        [HarmonyLib.HarmonyPostfix()]
        private static void UIPlayerMessageDisplay_Awake_Postfix(UIPlayerMessageDisplay __instance)
        {
            _PlayerMessageDisplay = __instance;
        }

        [HarmonyLib.HarmonyPatch(typeof(AutomationItemDispenser), "Tick")]
        [HarmonyLib.HarmonyPrefix()]
        private static bool AutomationItemDispenser_Tick_Prefix(float deltaTime, AutomationItemDispenser __instance, AutomationGridSpace ____space, AutomationGridSpace ____proxySpace, MasterItemList ____masterItemList, ref float ____timeToNextDrop, float ____timePerDrop, AutomationSystem ____system)
        {
            if (____space.Powered)
            {
                ChestLink[] links = _ChestLinks.Where(c => c.InputAutomationDispenser == __instance || c.OutputAutomationDispenser == __instance).ToArray();

                if (links.Any())
                {
                    foreach (ChestLink link in links)
                    {
                        if (link.InputAutomationDispenser == null || link.OutputAutomationDispenser == null)
                            continue;

                        if (link.InputAutomationDispenser == __instance)
                        {
                            AutomationCoreItem item = ____proxySpace != null ? ____proxySpace.ActiveItem : ____space.ActiveItem;
                            if (item != null && !item.IsMoving)
                            {
                                CoreItem itemById = ____masterItemList.GetItemByID<Isto.Atrio.CoreItem>(item.ID, "");

                                int num = __instance.GetInventory().Add(itemById, item.count, true);
                                if (num == item.count)
                                    ____system.DeleteCoreItem(item);
                                else if (num != 0)
                                    ____space.ActiveItem.count -= num;

                            }

                            ____timeToNextDrop -= deltaTime;
                            if (____timeToNextDrop <= 0 && __instance.GetInventory().GetTotalNumberOfItems() > 0)
                            {
                                bool anyRemoved = false;
                                for (int i = 0; i < __instance.GetInventory().PileCount; i++)
                                {
                                    ItemPile pile = __instance.GetInventory().GetItemAt(i);
                                    if (pile != null && pile.HasItems())
                                    {
                                        int num2 = link.OutputAutomationDispenser.GetInventory().Add(pile.item, pile.count, true);
                                        if (num2 > 0)
                                        {
                                            _ = __instance.GetInventory().RemoveAt(i, num2);
                                            anyRemoved = true;
                                        }
                                    }
                                }

                                if (anyRemoved)
                                    ____timeToNextDrop = ____timePerDrop;
                            }
                        }
                        else if (link.OutputAutomationDispenser == __instance)
                        {
                            ____timeToNextDrop -= deltaTime;
                            if (____timeToNextDrop <= 0f && (bool)AutomationItemDispenser_TryDropItem_Method.Invoke(__instance, new object[0]))
                                ____timeToNextDrop = ____timePerDrop;
                        }
                    }

                    return false; //Override default method.
                }
            }


            return true;
        }

        [HarmonyLib.HarmonyPatch(typeof(GameState), "LoadGameStateData")]
        [HarmonyLib.HarmonyPostfix()]
        private static void GameState_LoadGameStateData_Postfix(int saveSlot)
        {
            GameSaveSlot = saveSlot;
            LoadData(saveSlot);
        }

        [HarmonyLib.HarmonyPatch(typeof(GameState), "SaveGameStateData")]
        [HarmonyLib.HarmonyPostfix()]
        private static void GameState_SaveGameStateData_Postfix(int ____saveSlot)
        {
            SaveData(____saveSlot);
        }

        #endregion

        private class ChestLink
        {
            public SerializeableVector3 InputPosition { get; set; }
            public SerializeableVector3 OutputPosition { get; set; }

            [Newtonsoft.Json.JsonIgnore()]
            public DispenseChest InputChest { get; set; }

            [Newtonsoft.Json.JsonIgnore()]
            public AutomationItemDispenser InputAutomationDispenser { get; set; }

            [Newtonsoft.Json.JsonIgnore()]
            public DispenseChest OutputChest { get; set; }

            [Newtonsoft.Json.JsonIgnore()]
            public AutomationItemDispenser OutputAutomationDispenser { get; set; }
        }
    }
}
