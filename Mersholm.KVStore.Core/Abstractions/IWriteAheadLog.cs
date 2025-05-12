namespace Mersholm.KVStore.Core.Abstractions
{
    public interface IWriteAheadLog
    {
        void Append(char operation, string key, byte[] value);
        void ReplayTo(IKeyValueStore store);
        void Clear();
    }
}
