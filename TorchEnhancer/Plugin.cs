using BepInEx;

namespace TorchEnhancer
{
    [BepInPlugin("tech.zinals.atrio.torchenhancer", "Torch Enhancer", "1.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        internal static Plugin Instance { get; private set; }

        private HarmonyLib.Harmony _Harmony;

        public BepInEx.Configuration.ConfigEntry<float> TorchRange;
        public BepInEx.Configuration.ConfigEntry<float> TorchIntensity;


        public Plugin()
        {
            Instance = this;
            TorchRange = Config.Bind("Super Torch", "Range", 9f, "How much range should the super torch have? (Game default is 9, 500 is enough to light the would map)");
            TorchIntensity = Config.Bind("Super Torch", "Intensity", 1.25f, "How much should the torch intensify the light? (Game default is 1.25, 10 is enough to brighten the whole map)");
        }

        private void Awake()
        {
            _Harmony = new HarmonyLib.Harmony("tech.zinals.atrio.torchenhancer");
            _Harmony.PatchAll(typeof(Patches));
        }
    }
}
