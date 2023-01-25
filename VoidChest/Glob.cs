using CModLib.SaveLoad;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace VoidChest
{
    internal static class Glob
    {
        internal static BepInEx.Logging.ManualLogSource Logger { get; set; }
        internal static String PluginLocation { get; set; }
        internal static SaveManager SaveManager { get; set; }
        internal static SimpleListSaveData<Vector3> VoidChestSaveData { get; set; }

        internal static Dictionary<Vector3, bool> VoidChestsLoaded { get; private set; } = new Dictionary<Vector3, bool>();
    }
}
