using BepInEx;

namespace BasicQoL
{
    [BepInPlugin("tech.zinals.atrio.basicqol", "Basic QoL Improvements", "1.0.2")]
    public class Plugin : BaseUnityPlugin
    {
        private HarmonyLib.Harmony _Harmony;

        internal static BepInEx.Logging.ManualLogSource PLogger { get; private set; }

        public Plugin()
        {
            PLogger = Logger;
            Configs.Init(Config);
        }

        private void Awake()
        {
            _Harmony = new HarmonyLib.Harmony("tech.zinals.atrio.basicqol");
            _Harmony.PatchAll(typeof(Patches));
        }
    }
}
