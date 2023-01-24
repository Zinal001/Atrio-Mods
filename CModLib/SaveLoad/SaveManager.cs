using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CModLib.SaveLoad
{
    public class SaveManager
    {
        /// <summary>
        /// The settings of the internal json serializer.
        /// <para>Used by <see cref="SimpleListSaveData{T}"/> and <see cref="SimpleSaveData{T}"/> automatically.</para>
        /// <para>New type convertes can be added to <c>SaveManager.JsonSettings.Converters</c>.</para>
        /// </summary>
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

        /// <summary>
        /// The directory where all registered SaveData handlers should save data
        /// </summary>
        public string SavesDirectory { get; private set; }

        /// <summary>
        /// All registered SaveData handlers
        /// </summary>
        public List<SaveData> SaveDatas { get; private set; } = new List<SaveData>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pluginAssembly">The assembly of the plugin, can usually be gotten by called <see cref="System.Reflection.Assembly.GetExecutingAssembly"/></param>
        public SaveManager(System.Reflection.Assembly pluginAssembly) : this(System.IO.Path.GetDirectoryName(pluginAssembly.Location)) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pluginLocation">The plugins directory</param>
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

        /// <summary>
        /// Register a custom SaveData handler type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Register<T>() where T : SaveData => Register(Activator.CreateInstance<T>());

        /// <summary>
        /// Register a custom SaveData handler type with a specific instance
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <returns></returns>
        public T Register<T>(T instance) where T : SaveData
        {
            if (instance == default(T))
                return default;

            SaveDatas.Add(instance);
            return instance;
        }

        /// <summary>
        /// Register a SaveData for a single object (Can be a list or dictionary, but <see cref="SimpleListSaveData{T}"/> would be better suited)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filename"></param>
        /// <returns></returns>
        public SimpleSaveData<T> RegisterSimple<T>(String filename)
        {
            SimpleSaveData<T> data = SimpleSaveData<T>.Create(filename);
            SaveDatas.Add(data);
            return data;
        }

        /// <summary>
        /// Register a SaveData for a collection of objects (Of the same type)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filename"></param>
        /// <returns></returns>
        public SimpleListSaveData<T> RegisterSimpleList<T>(String filename)
        {
            SimpleListSaveData<T> data = SimpleListSaveData<T>.Create(filename);
            SaveDatas.Add(data);
            return data;
        }

        /// <summary>
        /// Unregister a SaveData handler
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool Unregister<T>() where T : SaveData
        {
            T saveData = SaveDatas.FirstOrDefault(s => s.GetType() == typeof(T)) as T;
            if (saveData != default(T))
                return Unregister(saveData);

            return false;
        }

        /// <summary>
        /// Unregister a specific SaveData handler
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool Unregister<T>(T instance) where T : SaveData
        {
            return SaveDatas.Remove(instance);
        }

        /// <summary>
        /// Save all data from every registered SaveData handler.
        /// <para>This is usually called in a Postfix of <see cref="Isto.Atrio.GameState.SaveGameStateData"/></para>
        /// </summary>
        /// <param name="slotNumber"></param>
        public void SaveAll(int slotNumber)
        {
            foreach (SaveData data in SaveDatas)
                data.Save(slotNumber, this);
        }

        /// <summary>
        /// load all data from every registered SaveData handler.
        /// <para>This is usually called in a Postfix of <see cref="Isto.Atrio.GameState.LoadGameStateData"/></para>
        /// </summary>
        /// <param name="slotNumber"></param>
        public void LoadAll(int slotNumber)
        {
            foreach (SaveData data in SaveDatas)
                data.Load(slotNumber, this);
        }


    }
}
