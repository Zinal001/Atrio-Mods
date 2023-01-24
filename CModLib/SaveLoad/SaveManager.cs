using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CModLib.SaveLoad
{
    public class SaveManager
    {
        public static Newtonsoft.Json.JsonSerializerSettings JsonSettings { get; private set; }

        static SaveManager()
        {
            JsonSettings = new Newtonsoft.Json.JsonSerializerSettings() 
            { 
                Formatting = Newtonsoft.Json.Formatting.Indented, 
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto 
            };

            JsonSettings.Converters.Add(new Converters.Vector3JsonConverter());
        }

        public string SavesDirectory { get; private set; }
        public List<SaveData> SaveDatas { get; private set; } = new List<SaveData>();

        public SaveManager(System.Reflection.Assembly pluginAssembly) : this(System.IO.Path.GetDirectoryName(pluginAssembly.Location)) { }

        public SaveManager(string pluginLocation)
        {
            SavesDirectory = System.IO.Path.Combine(GetBepInExDirectory(pluginLocation), "Saves");
        }

        private static String GetBepInExDirectory(String pluginLocation)
        {
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(pluginLocation);
            while (dir.Name != "BepInEx" && dir != null)
                dir = dir.Parent;

            if (dir == null)
                return pluginLocation;

            return dir.FullName;
        }

        public T Register<T>() where T : SaveData => Register(Activator.CreateInstance<T>());

        public T Register<T>(T instance) where T : SaveData
        {
            if (instance == default(T))
                return default;

            SaveDatas.Add(instance);
            return instance;
        }

        public SimpleSaveData<T> RegisterSimple<T>(String filename)
        {
            SimpleSaveData<T> data = SimpleSaveData<T>.Create(filename);
            SaveDatas.Add(data);
            return data;
        }

        public SimpleListSaveData<T> RegisterSimpleList<T>(String filename)
        {
            SimpleListSaveData<T> data = SimpleListSaveData<T>.Create(filename);
            SaveDatas.Add(data);
            return data;
        }

        public bool Unregister<T>() where T : SaveData
        {
            T saveData = SaveDatas.FirstOrDefault(s => s.GetType() == typeof(T)) as T;
            if (saveData != default(T))
                return Unregister(saveData);

            return false;
        }

        public bool Unregister<T>(T instance) where T : SaveData
        {
            return SaveDatas.Remove(instance);
        }

        public void SaveAll(int slotNumber)
        {
            foreach (SaveData data in SaveDatas)
                data.Save(slotNumber, this);
        }

        public void LoadAll(int slotNumber)
        {
            foreach (SaveData data in SaveDatas)
                data.Load(slotNumber, this);
        }


    }
}
