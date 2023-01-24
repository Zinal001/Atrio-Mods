using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CModLib.SaveLoad
{
    public class SimpleListSaveData<T> : SaveData, IList<T>
    {
        public override string FileName => _Filename;
        private String _Filename;

        private List<T> _List = new List<T>();

        public int Count => _List.Count;

        public bool IsReadOnly => false;

        public T this[int index] 
        {
            get => _List[index];
            set => _List[index] = value;
        }

        internal SimpleListSaveData(String saveFileName)
        {
            _Filename = saveFileName;
        }

        public static SimpleListSaveData<T> Create(String filename)
        {
            return new SimpleListSaveData<T>(filename);
        }

        protected override bool OnLoad(string filename, SaveManager saveManager)
        {
            try
            {
                if (File.Exists(filename))
                {
                    String json = File.ReadAllText(filename);
                    List<T> value = Newtonsoft.Json.JsonConvert.DeserializeObject<List<T>>(json, SaveManager.JsonSettings);
                    if (value != null)
                    {
                        _List = value;
                        return true;
                    }
                }
            }
            catch(Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to load data from {filename}: {ex.Message}");
            }

            return false;
        }

        protected override bool OnSave(string filename, SaveManager saveManager)
        {
            try
            {
                String json = Newtonsoft.Json.JsonConvert.SerializeObject(_List, SaveManager.JsonSettings);
                File.WriteAllText(filename, json);
                return true;
            }
            catch(Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to save data to {filename}: {ex.Message}");
            }

            return false;
        }

        public int IndexOf(T item) => _List.IndexOf(item);

        public void Insert(int index, T item) => _List.Insert(index, item);

        public void RemoveAt(int index) => _List.RemoveAt(index);

        public void Add(T item) => _List.Add(item);

        public void Clear() => _List.Clear();

        public bool Contains(T item) => _List.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => _List.CopyTo(array, arrayIndex);

        public bool Remove(T item) => _List.Remove(item);

        public IEnumerator<T> GetEnumerator() => _List.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _List.GetEnumerator();
    }
}
