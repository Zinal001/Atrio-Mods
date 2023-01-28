using BepInEx;
using System.IO;

namespace DispenseChestTeleporter
{
    [BepInPlugin("tech.zinals.atrio.dispensechestteleporter", "Dispense Chest Teleporter", "1.0.5")]
    public class Plugin : BaseUnityPlugin
    {
        internal static string PluginLocation { get; private set; }
        internal static BepInEx.Logging.ManualLogSource Log { get; private set; }


        private HarmonyLib.Harmony _Harmony;

        public Plugin()
        {
            Log = Logger;
            PluginLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Configs.Init(Config);
            DispenseChestHandler.Init(PluginLocation);
        }

        private void Awake()
        {
            _Harmony = new HarmonyLib.Harmony("tech.zinals.atrio.dispensechestteleporter");
            _Harmony.PatchAll(typeof(DispenseChestHandler));
        }
    }
}
