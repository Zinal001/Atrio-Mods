using Isto.Atrio;
using Isto.Atrio.AIBehaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

namespace BasicQoL
{
    static class Patches
    {
        private static System.Reflection.MethodInfo FoodInRange_TryGetDisplayObjectForHarvestable_Method = HarmonyLib.AccessTools.Method(typeof(FoodInRange), "TryGetDisplayObjectForHarvestable");

        [HarmonyLib.HarmonyPatch(typeof(PickerPalSleep), "GetFoodPosition")]
        [HarmonyLib.HarmonyPostfix()]
        private static void PickerPalSleep_GetFoodPosition_Prefix(PickerPalSleep __instance, List<Vector3> possiblePositions, ref Vector3 __result)
        {
            if(Configs.EnhancePickerPalPathFinding.Value)
            {
                Vector3 closestPosition = possiblePositions.OrderBy(p => Vector3.Distance(__instance.transform.position, p)).FirstOrDefault();
                __result = closestPosition;
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(FoodInRange), "TryFindFood")]
        [HarmonyLib.HarmonyPrefix()]
        private static bool FoodInRange_TryFindFood_Prefix(FoodInRange __instance, ref Vector3 foodPosition, ref bool __result, Vector3Int ____topLeftCorner, Vector3Int ____botRightCorner, HashSet<string> ____foodItemIDSet, ref List<Vector3> ____foodPositionsBuffer, NavMeshAgent ___agent)
        {
            if(Configs.EnhancePickerPalPathFinding.Value)
            {
                FoodInRange.FindFoodPositionsInArea(__instance.gameSystems.Value.automationSystem, ____topLeftCorner, ____botRightCorner, ____foodItemIDSet, ref ____foodPositionsBuffer);

                if(__instance.homeHive != null)
                {
                    PickerPalStationConfig pickerPalStationConfig = __instance.pickerConfig;
                    if(pickerPalStationConfig == null || pickerPalStationConfig.harvestOptions == null)
                    {
                        if (____foodPositionsBuffer.Remove(__instance.targetFoodPosition.Value) && !____foodPositionsBuffer.Any())
                        {
                            foodPosition = __instance.homePosition.Value + UnityUtils.RandomXZVector(__instance.searchRadiusFromStart.Value);
                            __result = true;
                            return false;
                        }
                    }

                    if (!____foodPositionsBuffer.Any())
                    {
                        foodPosition = Vector3.zero;
                        __result = false;
                        return false;
                    }

                    Vector3 fromPos = __instance.homePosition.Value;
                    if (___agent != null)
                        fromPos = ___agent.transform.position;

                    foodPosition = ____foodPositionsBuffer.OrderBy(p => Vector3.Distance(fromPos, p)).FirstOrDefault();

                    object[] methodArgs = new object[] { foodPosition, Vector3.zero };
                    if (UnityUtils.IsWorldPositionOnScreen(foodPosition, Camera.main) && (bool)FoodInRange_TryGetDisplayObjectForHarvestable_Method.Invoke(__instance, methodArgs))
                        foodPosition = (Vector3)methodArgs[1];

                    if (___agent != null && ___agent.enabled)
                    {
                        NavMeshPath path = new NavMeshPath();
                        __result = ___agent.CalculatePath(foodPosition + __instance.positionOffset, path);
                        return false;
                    }

                    __result = true;
                    return false;
                }
            }

            return true;
        }
    }
}
