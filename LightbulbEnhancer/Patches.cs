using Isto.Atrio;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LightbulbEnhancer
{
    static class Patches
    {
        private static Dictionary<PowerTethered, bool> _Tethers = new Dictionary<PowerTethered, bool>();
        private static Dictionary<Light, bool> _Lights = new Dictionary<Light, bool>();

        [HarmonyLib.HarmonyPatch(typeof(LightSystemLight), "Awake")]
        [HarmonyLib.HarmonyPostfix()]
        private static void LightSystemLight_Awake_Postfix(Light ____light)
        {
            if (____light != null && !_Lights.ContainsKey(____light))
            {
                ____light.range *= Plugin.Instance.LightbulbRangeMultiplier.Value;
                _Lights[____light] = true;
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(LightSystem), "OnLightAdded")]
        [HarmonyLib.HarmonyPostfix()]
        private static void LightSystem_OnLightAdded(LightSystemEventArgs e)
        {
            if(!e.moving)
            {
                if (!_Lights.ContainsKey(e.light))
                {
                    e.light.range *= Plugin.Instance.LightbulbRangeMultiplier.Value;
                    _Lights[e.light] = true;
                }
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(TetheredLight), "Awake")]
        [HarmonyLib.HarmonyPostfix()]
        private static void TetheredLight_Awake_Postfix(TetheredLight __instance, PowerTethered ____tether)
        {
            if (____tether != null && !_Tethers.ContainsKey(____tether))
            {
                ____tether.maxRange *= Plugin.Instance.LightbulbRangeMultiplier.Value;
                _Tethers[____tether] = true;
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(PowerTethered), "UpdateNearest")]
        [HarmonyLib.HarmonyPrefix()]
        private static void PowerTethered_UpdateNearest_Prefix(PowerTethered __instance)
        {
            if(!_Tethers.ContainsKey(__instance))
            {
                __instance.maxRange *= Plugin.Instance.LightbulbRangeMultiplier.Value;
                _Tethers[__instance] = true;
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(TetherSystem), "Add")]
        [HarmonyLib.HarmonyPrefix()]
        private static void TetherSystem_Add_Prefix(PowerTethered tethered)
        {
            if (!_Tethers.ContainsKey(tethered))
            {
                tethered.maxRange *= Plugin.Instance.LightbulbRangeMultiplier.Value;
                _Tethers[tethered] = true;
            }
        }

    }
}
