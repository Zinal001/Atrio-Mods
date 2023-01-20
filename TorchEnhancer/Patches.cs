using Isto.Atrio;
using System;
using System.Collections.Generic;
using System.Text;

namespace TorchEnhancer
{
    static class Patches
    {
        private static readonly System.Reflection.MethodInfo _RechargeableTorch_UpdateTorchTimer_Method = HarmonyLib.AccessTools.DeclaredMethod(typeof(RechargeableTorch), "UpdateTorchTimer");

        [HarmonyLib.HarmonyPatch(typeof(RechargeableTorch), "Update")]
        [HarmonyLib.HarmonyPrefix()]
        private static bool RechargeableTorch_OnUpdate_Prefix(RechargeableTorch __instance, PlayerController ____player, bool ____torchActive, ref float ____startRange, ref float ____startIntensity, float ____timeRemaining, ILightSystem ____lightSystem)
        {
            if(__instance.IsEnabled && ____torchActive)
            {
                if (____lightSystem.IsNonMovingLightNear(____player.transform.position) || ____timeRemaining <= 0f)
                    return true;

                ____startRange = Plugin.Instance.TorchRange.Value;
                ____startIntensity = Plugin.Instance.TorchIntensity.Value;

                _RechargeableTorch_UpdateTorchTimer_Method.Invoke(__instance, new object[0]);
                return false;
            }

            return true;
        }
    }
}
