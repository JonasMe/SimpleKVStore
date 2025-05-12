namespace Mersholm.KVStore.Core.Abstractions
{
    public interface ISnapshotProvider
    {
        void SaveSnapshot(IKeyValueStore store);
        void LoadSnapshot(IKeyValueStore store);
    }
}
