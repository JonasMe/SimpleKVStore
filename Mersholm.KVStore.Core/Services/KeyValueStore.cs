using Mersholm.KVStore.Core.Abstractions;
using MessagePack;
using System.Collections.Concurrent;

namespace Mersholm.KVStore.Core.Services
{
    public class KeyValueStore : IKeyValueStore
    {
        private readonly ConcurrentDictionary<string, byte[]> store = new();
        private readonly IWriteAheadLog wal;
        private readonly ISnapshotProvider snapshot;

        public KeyValueStore(IWriteAheadLog wal, ISnapshotProvider snapshot)
        {
            this.wal = wal;
            this.snapshot = snapshot;
            snapshot.LoadSnapshot(this);
            wal.ReplayTo(this);
        }

        public void SaveData(string key, object value)
        {
            byte[] valueBytes = MessagePackSerializer.Serialize(value);
            SetDataDirect(key, valueBytes);
            wal.Append('S', key, valueBytes);
        }

        public bool DeleteData(string key)
        {
            if (DeleteDataDirect(key))
            {
                wal.Append('R', key, null);
                return true;
            }
            return false;
        }

        public object GetData(string key)
        {
            if (store.TryGetValue(key, out var valueBytes))
            {
                return MessagePackSerializer.Deserialize<object>(valueBytes);
            }
            return null;
        }

        public IEnumerable<string> GetKeys() => store.Keys;

        public void PersistStore()
        {
            snapshot.SaveSnapshot(this);
            wal.Clear();
        }

        internal void SetDataDirect(string key, byte[] valueBytes)
        {
            store[key] = valueBytes;
        }

        internal bool DeleteDataDirect(string key)
        {
            return store.TryRemove(key, out _);
        }
        internal bool TryGetRawData(string key, out byte[] rawBytes)
        {
            return store.TryGetValue(key, out rawBytes);
        }
    }
}
