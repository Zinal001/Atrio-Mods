using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CModLib.SaveLoad
{
    public class SimpleSaveData<T> : SaveData
    {
        public override string FileName => _Filename;

        private String _Filename;

        public T Value { get; set; }

        internal SimpleSaveData(String saveFileName)
        {
            _Filename = saveFileName;
        }

        public static SimpleSaveData<T> Create(String filename)
        {
            return new SimpleSaveData<T>(filename);
        }

        protected override bool OnLoad(string filename, SaveManager saveManager)
        {
            if(File.Exists(filename))
            {
                String json = File.ReadAllText(filename);
                T value = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json, SaveManager.JsonSettings);
                if (!default(T).Equals(value))
                {
                    Value = value;
                    return true;
                }
            }

            return false;
        }

        protected override bool OnSave(string filename, SaveManager saveManager)
        {
            String json = Newtonsoft.Json.JsonConvert.SerializeObject(Value, SaveManager.JsonSettings);
            File.WriteAllText(filename, json);
            return true;
        }
    }
}
