using BepInEx;

namespace LightbulbEnhancer
{
    [BepInPlugin("tech.zinals.atrio.lightbulbenhancer", "Lightbulb Enhancer", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static Plugin Instance { get; private set; }

        private HarmonyLib.Harmony _Harmony;

        public BepInEx.Configuration.ConfigEntry<float> LightbulbRangeMultiplier;

        public Plugin()
        {
            Instance = this;
            LightbulbRangeMultiplier = Config.Bind("Light System", "Range Multiplier", 1f, "Multiplier for the range of each lightbulb");
        }

        private void Awake()
        {
            _Harmony = new HarmonyLib.Harmony("tech.zinals.atrio.torchenhancer");
            _Harmony.PatchAll(typeof(Patches));
        }
    }
}
