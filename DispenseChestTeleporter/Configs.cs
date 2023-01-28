using System;
using System.Collections.Generic;
using System.Text;

namespace DispenseChestTeleporter
{
    public static class Configs
    {
        private static BepInEx.Configuration.ConfigFile _ConfigFile;

        public static BepInEx.Configuration.ConfigEntry<float> MaxLinkRange { get; private set; }

        public static void Init(BepInEx.Configuration.ConfigFile configFile)
        {
            _ConfigFile = configFile;
            MaxLinkRange = _ConfigFile.Bind("Link", "Max Range", 0f, "The maximum range of each link (Set to 0 for unlimited)");
        }

    }
}
