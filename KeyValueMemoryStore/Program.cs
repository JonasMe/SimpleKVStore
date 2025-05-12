using Mersholm.KVStore.Core.Abstractions;
using Mersholm.KVStore.Core.Services;
using Mersholm.KVStore.Core.Snapshots;
using Mersholm.KVStore.Core.WAL;

class Program
{
    const string WAL_PATH = "_wal.data";
    static readonly string SNAPSHOT_PATH = "_snapshot.data";

    static async Task Main(string[] args)
    {
        ISnapshotProvider snapshotProvider = new BinarySnapshotProvider(SNAPSHOT_PATH);
        IWriteAheadLog writeAheadLog = new FileWriteAheadLog(WAL_PATH);
        IKeyValueStore keyValueStore = new KeyValueStore(writeAheadLog, snapshotProvider);

        Console.WriteLine("Available commands: SET, GET, DELETE, LIST, PERSIST, EXIT.");

        while (true)
        {
            Console.Write("> ");
            string? input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input)) continue;

            var parts = input.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            string command = parts[0].ToUpper();
            switch (command)
            {
                case "SET":
                    if (parts.Length < 3)
                    {
                        Console.WriteLine("Usage: SET {key} {value}");
                        continue;
                    }

                    keyValueStore.SaveData(parts[1], parts[2]);
                    Console.WriteLine($"Set {parts[1]} = {parts[2]}");

                    break;

                case "GET":
                    if (parts.Length < 2)
                    {
                        Console.WriteLine("Usage: GET {key}");
                        continue;
                    }
                    object value = keyValueStore.GetData(parts[1]);

                    if (value != null)
                        Console.WriteLine($"{parts[1]} = {value}");
                    else
                        Console.WriteLine($"Data not found for: '{parts[1]}'");
                    break;

                case "DELETE":
                    if (parts.Length < 2)
                    {
                        Console.WriteLine("Usage: DELETE {key}");
                        continue;
                    }

                    if (keyValueStore.DeleteData(parts[1]))
                        Console.WriteLine($"Deleted key '{parts[1]}'.");
                    else
                        Console.WriteLine($"Key '{parts[1]}' not found.");
                    break;

                case "LIST":
                    Console.WriteLine("Stored keys:");
                    foreach (var key in keyValueStore.GetKeys())
                        Console.WriteLine($"{key}");
                    break;

                case "PERSIST":
                    Console.WriteLine("Snapshotting store");
                    keyValueStore.PersistStore();
                    break;

                case "EXIT":
                    return;

                default:
                    Console.WriteLine("Unknown command. Available commands: SET, GET, DELETE, LIST, PERSIST, EXIT.");
                    break;
            }
        }
    }
}