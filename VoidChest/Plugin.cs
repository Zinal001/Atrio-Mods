using BepInEx;
using CModLib.SaveLoad;
using I2.Loc;
using UnityEngine;

namespace VoidChest
{
    [BepInPlugin("tech.zinals.atrio.voidchest", "Void Chest", "1.0.2")]
    public class Plugin : BaseUnityPlugin
    {
        private HarmonyLib.Harmony _Harmony;

        public Plugin()
        {
            Glob.Logger = Logger;
            Glob.PluginLocation = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Glob.SaveManager = new SaveManager(Glob.PluginLocation);
            Glob.VoidChestSaveData = Glob.SaveManager.RegisterSimpleList<Vector3>("VoidChests");
        }

        private void Awake()
        {
            _Harmony = new HarmonyLib.Harmony("tech.zinals.atrio.voidchest");
            _Harmony.PatchAll(typeof(Patches));

            CModLib.Loc.SetTranslation("VOIDCHEST_NAME", "Void Chest");
            CModLib.Loc.SetTranslation("VOIDCHEST_DESCRIPTION", "A chest that will destroy any item that is put into it.");
            CModLib.Loc.SetTranslation("VOIDCHEST_TOOLTIP", "Void Chest...");
        }
    }
}
