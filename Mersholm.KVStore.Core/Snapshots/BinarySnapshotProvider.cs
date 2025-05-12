using Mersholm.KVStore.Core.Abstractions;
using Mersholm.KVStore.Core.Services;
using MessagePack;
using System.Text;

namespace Mersholm.KVStore.Core.Snapshots
{
    public class BinarySnapshotProvider : ISnapshotProvider
    {
        private readonly string snapshotPath;

        public BinarySnapshotProvider(string snapshotPath)
        {
            this.snapshotPath = snapshotPath;
        }

        public void SaveSnapshot(IKeyValueStore store)
        {
            using (FileStream fs = new FileStream(snapshotPath, FileMode.Create, FileAccess.Write))
            {
                foreach (var key in store.GetKeys())
                {
                    byte[] keyBytes = Encoding.UTF8.GetBytes(key);
                    byte[] valueBytes = GetRawDataBytes(store, key);
                    fs.Write(BitConverter.GetBytes(keyBytes.Length)); 
                    fs.Write(keyBytes);                             
                    fs.Write(BitConverter.GetBytes(valueBytes.Length)); 
                    fs.Write(valueBytes);                           
                }
            }
        }

        public void LoadSnapshot(IKeyValueStore store)
        {
            if (!File.Exists(snapshotPath)) return;

            using (FileStream fs = new FileStream(snapshotPath, FileMode.Open))
            {
                while (fs.Position < fs.Length)
                {
                    int keyLength = ReadInt(fs);
                    string key = ReadString(fs, keyLength);
                    int valueLength = ReadInt(fs);
                    byte[] valueBytes = new byte[valueLength];
                    fs.Read(valueBytes);

                    object value = MessagePackSerializer.Deserialize<object>(valueBytes);

                    store.SaveData(key, value);
                }
            }
        }

        private byte[] GetRawDataBytes(IKeyValueStore store, string key)
        {
            var keyValueStore = store as KeyValueStore;
            if (keyValueStore == null) throw new InvalidOperationException("Store must be of correct type.");

            if (keyValueStore.TryGetRawData(key, out var rawBytes))
            {
                return rawBytes;
            }

            throw new InvalidOperationException($"Failed to retrieve raw data for {key}");
        } 

        private int ReadInt(FileStream fs)
        {
            Span<byte> buffer = stackalloc byte[4];
            fs.Read(buffer);
            return BitConverter.ToInt32(buffer);
        }

        private string ReadString(FileStream fs, int length)
        {
            byte[] buffer = new byte[length];
            fs.Read(buffer);
            return Encoding.UTF8.GetString(buffer);
        }
    }
}
