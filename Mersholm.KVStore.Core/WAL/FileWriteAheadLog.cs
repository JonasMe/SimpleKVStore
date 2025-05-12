using Mersholm.KVStore.Core.Abstractions;
using Mersholm.KVStore.Core.Services;
using System.Text;

namespace Mersholm.KVStore.Core.WAL
{
    public class FileWriteAheadLog : IWriteAheadLog
    {
        private readonly string walPath;

        public FileWriteAheadLog(string walPath)
        {
            this.walPath = walPath;
        }

        public void Append(char operation, string key, byte[] value)
        {
            using (FileStream fs = new FileStream(walPath, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, true))
            {
                int keyLength = Encoding.UTF8.GetByteCount(key);
                int valueLength = value?.Length ?? 0;
                Span<byte> buffer = stackalloc byte[1 + 4 + keyLength + 4 + valueLength];

                unsafe
                {
                    fixed (byte* ptr = buffer)
                    {
                        ptr[0] = (byte)operation;
                        *(int*)(ptr + 1) = keyLength;
                        Encoding.UTF8.GetBytes(key).CopyTo(buffer.Slice(5));

                        if (value != null)
                        {
                            *(int*)(ptr + 5 + keyLength) = valueLength;
                            value.CopyTo(buffer.Slice(9 + keyLength));
                        }
                    }
                }

                fs.Write(buffer);
            }
        }

        public void ReplayTo(IKeyValueStore store)
        {
            if (!File.Exists(walPath)) return;

            using (var fs = new FileStream(walPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                while (fs.Position < fs.Length)
                {
                    Span<byte> buffer = stackalloc byte[1];
                    fs.Read(buffer);
                    char operation = (char)buffer[0];

                    int keyLength = ReadInt(fs);
                    string key = ReadString(fs, keyLength);

                    if (operation == 'S')
                    {
                        int valueLength = ReadInt(fs);
                        byte[] value = new byte[valueLength];
                        fs.Read(value);
                        ((KeyValueStore)store).SetDataDirect(key, value);
                    }
                    else if (operation == 'R')
                    {
                        ((KeyValueStore)store).DeleteDataDirect(key);
                    }
                }
            }
        }

        public void Clear()
        {
            if (File.Exists(walPath))
            {
                File.Delete(walPath);
                File.Create(walPath).Dispose();
            }
        }

        private int ReadInt(FileStream fs)
        {
            Span<byte> buffer = stackalloc byte[4];
            fs.Read(buffer);
            return BitConverter.ToInt32(buffer);
        }

        private string? ReadString(FileStream fs, int length)
        {
            if (length < 0 || length > 1024 * 1024)
                return null;

            Span<byte> buffer = length <= 1024 ? stackalloc byte[length] : new byte[length];
            fs.Read(buffer);
            return Encoding.UTF8.GetString(buffer);
        }
    }
}
