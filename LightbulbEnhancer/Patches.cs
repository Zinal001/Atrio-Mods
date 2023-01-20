using Isto.Atrio;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LightbulbEnhancer
{
    static class Patches
    {
        [HarmonyLib.HarmonyPatch(typeof(LightSystemLight), "Awake")]
        [HarmonyLib.HarmonyPostfix()]
        private static void LightSystemLight_Awake_Postfix(Light ____light)
        {
            if(____light != null)
                ____light.range *= Plugin.Instance.LightbulbRangeMultiplier.Value;
        }

        [HarmonyLib.HarmonyPatch(typeof(TetheredLight), "Awake")]
        [HarmonyLib.HarmonyPostfix()]
        private static void TetheredLight_Awake_Postfix(TetheredLight __instance, PowerTethered ____tether)
        {
            if (____tether != null)
                ____tether.maxRange *= Plugin.Instance.LightbulbRangeMultiplier.Value;
        }

    }
}
