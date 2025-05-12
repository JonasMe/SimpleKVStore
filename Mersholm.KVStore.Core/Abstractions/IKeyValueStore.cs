namespace Mersholm.KVStore.Core.Abstractions
{
    public interface IKeyValueStore
    {
        void SaveData(string key, object value);
        bool DeleteData(string key);
        object GetData(string key);
        IEnumerable<string> GetKeys();
        void PersistStore();
    }
}
