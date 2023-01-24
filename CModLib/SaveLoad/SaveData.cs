using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CModLib.SaveLoad
{
    public abstract class SaveData
    {
        public abstract string FileName { get; }

        public bool Save(int slotNumber, SaveManager saveManager)
        {
            Directory.CreateDirectory(Path.Combine(saveManager.SavesDirectory, slotNumber.ToString()));
            string filename = Path.Combine(saveManager.SavesDirectory, slotNumber.ToString(), $"{FileName}.json");
            return OnSave(filename, saveManager);
        }

        public bool Load(int slotNumber, SaveManager saveManager)
        {
            string filename = Path.Combine(saveManager.SavesDirectory, slotNumber.ToString(), $"{FileName}.json");
            return OnLoad(filename, saveManager);
        }

        protected abstract bool OnSave(string filename, SaveManager saveManager);
        protected abstract bool OnLoad(string filename, SaveManager saveManager);
    }
}
