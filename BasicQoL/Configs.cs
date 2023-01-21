using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace BasicQoL
{
    internal static class Configs
    {
        internal static ConfigEntry<bool> EnhancePickerPalPathFinding { get; private set; }

        internal static void Init(ConfigFile configFile)
        {
            EnhancePickerPalPathFinding = configFile.Bind("Picker Pal", "Enhance Path Finding", true, "Change the picker pals pathfinding to always grab the closest target instead of random.");
        }

    }
}
