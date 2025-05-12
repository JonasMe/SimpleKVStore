
# Simpel KV-store

### Tanker om løsning
Jeg ville gerne lave koden så skalerbar, fail-safe og performant som muligt på 2 timer. Som minimum ved at skrive til en WAL ved hjælp af direkte memory håndtering og udenom GC, på samme tid ville jeg ved min WAL skrivning gerne sørger for kun at arbejde på stacken og kun have én enkelt IO write uden overhead.
Jeg ville gerne undgå flaskehalse samt at der ikke kunne genoprettes nøglesæt hvis systemet gik ned.

### Forbedringer
Ved mere tid ville jeg gerne tilføje:
- Atomiske operationer
- Asynkron write/read til WAL og snapshot
- Tilføjelse af logging og bedre fejl-håndtering
- Gøre serialiseringen abstrakt så MessagePack kan skiftes ud
- TTL på nøgler
- Læse/skrive seperation af in-memory storen så jeg kan optimere på hentning.


----

## CORE Service
*Mersholm.KVStore.Core* indeholder den generelle logik for key-value storen.

### Generelt flow og beskrivelse
Data gemmes direkte i en in-memory ConcurrentDictionary<string, byte[]>. Alle operationer logges (tilføjelser, opdateringer, sletninger) sekventielt i Write-Ahead Log (WAL) på disk, hvilket giver mulighed for at genskabe alle ændringer der er foretaget siden sidste snapshot. Når snapshot-persisterings metoden kaldes (```PersistStore()```), gemmes hele in-memory dictionary til disk som en snapshot fil, hvilket repræsenterer en komplet og konsistent tilstand. Herefter nulstilles WAL, da alle ændringer nu er en del af det gemte snapshot.

#### Snapshots
Snapshots er den faktiske tilstand af key/value storen på et givet tidspunkt og er jf. specifikationen lagret på disken. Her gemmes alle nøgler og deres værdi i en enkelt fil for hurtigt at kunne indlæse HELE storen i memory uden at skulle afspille alle ændringer sekventielt fra WAL. Når denne gemmes ryddes WAL.

#### Write-Ahead Log (WAL)
En sekventiel logfil der også persisteres på disken. Her registreres alle ændringer (tilføjelser, opdateringer og sletninger) som sker, for at sikrer at ændringer lavet mellem snapshots kan gendannes. Ved programstart indlæses snapshot filen og herefter WAL som afspilles for at genskabe ændringer siden snapshot blev gemt.

### Eksempelkode:
```
// Opret Write-Ahead Log (WAL) og Snapshot provideren
ISnapshotProvider snapshotProvider = new BinarySnapshotProvider("_snapshot.data");
IWriteAheadLog writeAheadLog = new FileWriteAheadLog("_wal.data");

// Initialiser storen med WAL og snapshot providers.
IKeyValueStore keyValueStore = new KeyValueStore(writeAheadLog, snapshotProvider);

// Gem en værdi i storen (WAL og memory)
keyValueStore.SaveData("bruger","Hans");

// Hent en værdi fra memory
var value = keyValueStore.GetData("bruger");
Console.WriteLine($"Værdi: {value}");

// Slet en nøgle fra memory og opret et "removal" entry i WAL
keyValueStore.DeleteData("bruger");

// List alle nøgler
foreach (var key in keyValueStore.GetKeys())
{
    Console.WriteLine(key);
}

// Gem nuværende memory til snapshottet og ryd WAL.
keyValueStore.PersistStore();
```

----------

## Program.cs
Lavet til hurtigt at afprøve KV-store. Eksekver og kør nedenstående kommandoer :)

### Kommandoer:

-   `SET {key} {value}` - Gemmer værdi til pågældende nøgle.
    
-   `GET {key}` - Henter værdien for nøglen.
    
-   `DELETE {key}` - Sletter værdien for nøglen.
    
-   `LIST` - Viser alle nøgler.
    
-   `PERSIST` - Gemmer til snapshot.
    
-   `EXIT` - Afslutter programmet.