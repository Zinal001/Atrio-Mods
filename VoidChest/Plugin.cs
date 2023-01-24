using BepInEx;
using CModLib.SaveLoad;
using I2.Loc;
using UnityEngine;

namespace VoidChest
{
    [BepInPlugin("tech.zinals.atrio.voidchest", "Void Chest", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private HarmonyLib.Harmony _Harmony;

        public Plugin()
        {
            Glob.Logger = Logger;
            Glob.SaveManager = new SaveManager(System.Reflection.Assembly.GetExecutingAssembly());
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
