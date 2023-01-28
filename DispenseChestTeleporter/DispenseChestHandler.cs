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
        internal static int GameSaveSlotBefore = -1;

        private static readonly System.Reflection.MethodInfo AutomationItemDispenser_TryDropItem_Method = HarmonyLib.AccessTools.DeclaredMethod(typeof(AutomationItemDispenser), "TryDropItem");

        private static DispenseChest _LastChestInteracted = null;
        private static DateTime _LastChestInteractedAt = DateTime.MinValue;
        private static Vector3? _CurrentInputPosition = null;
        private static AutomationItemDispenser _CurrentAutomationDispenser = null;
        private static UIPlayerMessageDisplay _PlayerMessageDisplay;

        private static Dictionary<AutomationItemDispenser, bool> _DispensersLoaded = null;

        private static CModLib.SaveLoad.SaveManager _SaveManager;
        private static CModLib.SaveLoad.SimpleListSaveData<ChestLink> _ChestLinks;

        internal static void Init(String pluginLocation)
        {
            _SaveManager = new CModLib.SaveLoad.SaveManager(pluginLocation);
            _ChestLinks = _SaveManager.RegisterSimpleList<ChestLink>("ChestLinks");
        }

        private static void SaveData(int slotNumber)
        {
            _SaveManager.SaveAll(slotNumber);
            Plugin.Log.LogDebug($"Saved {_ChestLinks.Count} chestlinks.");

            if(GameSaveSlotBefore != -1)
            {
                String filename = Path.Combine(Plugin.PluginLocation, $"{GameSaveSlotBefore}_ChestLinks.json");
                if (File.Exists(filename))
                {
                    File.Move(filename, Path.Combine(Plugin.PluginLocation, $"{GameSaveSlotBefore}_ChestLinks_Backup.json"));
                    Plugin.Log.LogDebug($"Moved old game data from slot {GameSaveSlotBefore} to new saveload system.");
                }
            }            
        }

        private static void LoadData(int slotNumber)
        {
            _SaveManager.LoadAll(slotNumber);

            String filename = Path.Combine(Plugin.PluginLocation, $"{slotNumber}_ChestLinks.json");
            if (File.Exists(filename))
            {
                String json = File.ReadAllText(filename);
                List<OldChestLink> links = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OldChestLink>>(json);
                if (links != null)
                {
                    foreach(OldChestLink link in links)
                    {
                        if (!_ChestLinks.Any(c => c.InputPosition == link.InputPosition && c.OutputPosition != link.OutputPosition))
                            _ChestLinks.Add(new ChestLink() { InputPosition = link.InputPosition, OutputPosition = link.OutputPosition });
                    }
                }
            }

            RemoveDuplicateLinks();
            Plugin.Log.LogDebug($"Loaded {_ChestLinks.Count} chestlinks.");

            _DispensersLoaded = new Dictionary<AutomationItemDispenser, bool>();
        }

        private static void RemoveDuplicateLinks()
        {
            int removed = 0;
            foreach(ChestLink link in _ChestLinks.ToArray())
            {
                ChestLink[] similarLinks = _ChestLinks.Where(c => c.InputPosition == link.InputPosition && c.OutputPosition == link.OutputPosition).ToArray();
                if(similarLinks.Length > 1)
                {
                    for (int i = 1; i < similarLinks.Length; i++)
                    {
                        if (_ChestLinks.Remove(similarLinks[i]))
                            removed++;
                    }
                }
            }

            if(removed > 0)
                Plugin.Log.LogDebug($"Removed {removed} duplicate links.");
        }

        private static int SetupTeleport(DispenseChest outputChest, AutomationItemDispenser outputAutomationDispenser)
        {
            ChestLink link = _ChestLinks.FirstOrDefault(c => c.InputAutomationDispenser == _CurrentAutomationDispenser && c.OutputAutomationDispenser == outputAutomationDispenser);
            ChestLink link2 = _ChestLinks.FirstOrDefault(c => c.InputAutomationDispenser == outputAutomationDispenser && c.OutputAutomationDispenser == _CurrentAutomationDispenser);


            if (link == null && link2 == null)
            {
                _ChestLinks.Add(new ChestLink()
                {
                    InputPosition = _CurrentInputPosition.Value,
                    OutputPosition = outputChest.transform.position,
                    InputAutomationDispenser = _CurrentAutomationDispenser,
                    OutputAutomationDispenser = outputAutomationDispenser
                });

                return 0;
            }
            else if(link2 != null)
            {
                return 1;
            }

            return 2;
        }

        #region Patches

        [HarmonyLib.HarmonyPatch(typeof(DispenseChest), "DoPickupAction")]
        [HarmonyLib.HarmonyPrefix()]
        private static void DispenseChest_DoPickupAction_Prefix(DispenseChest __instance, AutomationItemDispenser ____systemContainer)
        {
            int removed = 0;

            foreach (ChestLink link in _ChestLinks.Where(c => c.InputAutomationDispenser == ____systemContainer || c.OutputAutomationDispenser == ____systemContainer).ToArray())
            {
                if (_ChestLinks.Remove(link))
                    removed++;
            }

            if(removed > 0)
                Plugin.Log.LogDebug($"Removed {removed} links when this Dispense Chest was destroyed.");
        }

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

                if (_CurrentInputPosition == null)
                {
                    _CurrentInputPosition = __instance.transform.position;
                    _CurrentAutomationDispenser = ____systemContainer;
                    Plugin.Log.LogDebug($"Input specified!");

                    if (_PlayerMessageDisplay != null)
                        _PlayerMessageDisplay.DisplayMessage("Input specified successfully, select output Dispense Chest.", 6f);
                }
                else if (__instance.transform.position != _CurrentInputPosition)
                {
                    if(Configs.MaxLinkRange.Value > 0f)
                    {
                        float distance = Vector3.Distance(__instance.transform.position, _CurrentInputPosition.Value);
                        if(distance > Configs.MaxLinkRange.Value)
                        {
                            if (_PlayerMessageDisplay != null)
                                _PlayerMessageDisplay.DisplayMessage($"Link distance ({distance}) exceeds the maximum range of {Configs.MaxLinkRange.Value}.", 6f);

                            return false;
                        }
                    }

                    int status = SetupTeleport(__instance, ____systemContainer);
                    _CurrentInputPosition = null;
                    _CurrentAutomationDispenser = null;

                    String message = "Dispense Chest link successfully established!";

                    if (status == 0)
                        Plugin.Log.LogDebug($"Output specified, link created!");
                    else if (status == 1)
                        message = "An opposite link already exists.";
                    else
                        message = "This link is already setup.";

                    if (_PlayerMessageDisplay != null)
                        _PlayerMessageDisplay.DisplayMessage(message, 6f);
                }

                return false;
            }

            return true;
        }

        private static void SetupChestLinks(AutomationItemDispenser automationItemDispenser, AutomationItemDispenserData data)
        {
            if(data != null)
            {
                ChestLink[] links = _ChestLinks.Where(c => c.InputPosition.Equals(data.position) || c.OutputPosition.Equals(data.position)).ToArray();

                foreach(ChestLink link in links)
                {
                    if(link.InputPosition.Equals(data.position))
                    {
                        link.InputAutomationDispenser = automationItemDispenser;
                        Plugin.Log.LogDebug($"SetupChestLinks - {data.position} is INPUT");
                    }

                    if (link.OutputPosition.Equals(data.position))
                    {
                        link.OutputAutomationDispenser = automationItemDispenser;
                        Plugin.Log.LogDebug($"SetupChestLinks - {data.position} is OUTPUT");
                    }
                }

                _DispensersLoaded[automationItemDispenser] = true;
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(UIPlayerMessageDisplay), "Awake")]
        [HarmonyLib.HarmonyPostfix()]
        private static void UIPlayerMessageDisplay_Awake_Postfix(UIPlayerMessageDisplay __instance)
        {
            _PlayerMessageDisplay = __instance;
        }

        [HarmonyLib.HarmonyPatch(typeof(AutomationItemDispenser), "LoadData")]
        [HarmonyLib.HarmonyPostfix()]
        private static void AutomationItemDispenser_LoadData_Postfix(AutomationItemDispenser __instance, AutomationItemDispenserData data)
        {
            if (_DispensersLoaded != null && !_DispensersLoaded.ContainsKey(__instance))
                SetupChestLinks(__instance, data);
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
                        
                        if (link.OutputAutomationDispenser == __instance)
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
            GameSaveSlotBefore = GameSaveSlot;
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

        private class OldChestLink
        {
            public SerializeableVector3 InputPosition { get; set; }
            public SerializeableVector3 OutputPosition { get; set; }

            [Newtonsoft.Json.JsonIgnore()]
            public AutomationItemDispenser InputAutomationDispenser { get; set; }

            [Newtonsoft.Json.JsonIgnore()]
            public AutomationItemDispenser OutputAutomationDispenser { get; set; }
        }

        private class ChestLink
        {
            public Vector3 InputPosition { get; set; }
            public Vector3 OutputPosition { get; set; }

            [Newtonsoft.Json.JsonIgnore()]
            public AutomationItemDispenser InputAutomationDispenser { get; set; }

            [Newtonsoft.Json.JsonIgnore()]
            public AutomationItemDispenser OutputAutomationDispenser { get; set; }
        }
    }
}
