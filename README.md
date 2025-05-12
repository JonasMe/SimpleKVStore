
# Simpel KV-store

### Tanker om l�sning
Jeg ville gerne lave koden s� skalerbar, fail-safe og performant som muligt p� 2 timer. Som minimum ved at skrive til en WAL ved hj�lp af direkte memory h�ndtering og udenom GC, p� samme tid ville jeg ved min WAL skrivning gerne s�rger for kun at arbejde p� stacken og kun have �n enkelt IO write uden overhead.
Jeg ville gerne undg� flaskehalse samt at der ikke kunne genoprettes n�gles�t hvis systemet gik ned.

### Forbedringer
Ved mere tid ville jeg gerne tilf�je:
- Atomiske operationer
- Asynkron write/read til WAL og snapshot
- Tilf�jelse af logging og bedre fejl-h�ndtering
- G�re serialiseringen abstrakt s� MessagePack kan skiftes ud
- TTL p� n�gler
- L�se/skrive seperation af in-memory storen s� jeg kan optimere p� hentning.


----

## CORE Service
*Mersholm.KVStore.Core* indeholder den generelle logik for key-value storen.

### Generelt flow og beskrivelse
Data gemmes direkte i en in-memory ConcurrentDictionary<string, byte[]>. Alle operationer logges (tilf�jelser, opdateringer, sletninger) sekventielt i Write-Ahead Log (WAL) p� disk, hvilket giver mulighed for at genskabe alle �ndringer der er foretaget siden sidste snapshot. N�r snapshot-persisterings metoden kaldes (```PersistStore()```), gemmes hele in-memory dictionary til disk som en snapshot fil, hvilket repr�senterer en komplet og konsistent tilstand. Herefter nulstilles WAL, da alle �ndringer nu er en del af det gemte snapshot.

#### Snapshots
Snapshots er den faktiske tilstand af key/value storen p� et givet tidspunkt og er jf. specifikationen lagret p� disken. Her gemmes alle n�gler og deres v�rdi i en enkelt fil for hurtigt at kunne indl�se HELE storen i memory uden at skulle afspille alle �ndringer sekventielt fra WAL. N�r denne gemmes ryddes WAL.

#### Write-Ahead Log (WAL)
En sekventiel logfil der ogs� persisteres p� disken. Her registreres alle �ndringer (tilf�jelser, opdateringer og sletninger) som sker, for at sikrer at �ndringer lavet mellem snapshots kan gendannes. Ved programstart indl�ses snapshot filen og herefter WAL som afspilles for at genskabe �ndringer siden snapshot blev gemt.

### Eksempelkode:
```
// Opret Write-Ahead Log (WAL) og Snapshot provideren
ISnapshotProvider snapshotProvider = new BinarySnapshotProvider("_snapshot.data");
IWriteAheadLog writeAheadLog = new FileWriteAheadLog("_wal.data");

// Initialiser storen med WAL og snapshot providers.
IKeyValueStore keyValueStore = new KeyValueStore(writeAheadLog, snapshotProvider);

// Gem en v�rdi i storen (WAL og memory)
keyValueStore.SaveData("bruger","Hans");

// Hent en v�rdi fra memory
var value = keyValueStore.GetData("bruger");
Console.WriteLine($"V�rdi: {value}");

// Slet en n�gle fra memory og opret et "removal" entry i WAL
keyValueStore.DeleteData("bruger");

// List alle n�gler
foreach (var key in keyValueStore.GetKeys())
{
    Console.WriteLine(key);
}

// Gem nuv�rende memory til snapshottet og ryd WAL.
keyValueStore.PersistStore();
```

----------

## Program.cs
Lavet til hurtigt at afpr�ve KV-store. Eksekver og k�r nedenst�ende kommandoer :)

### Kommandoer:

-   `SET {key} {value}` - Gemmer v�rdi til p�g�ldende n�gle.
    
-   `GET {key}` - Henter v�rdien for n�glen.
    
-   `DELETE {key}` - Sletter v�rdien for n�glen.
    
-   `LIST` - Viser alle n�gler.
    
-   `PERSIST` - Gemmer til snapshot.
    
-   `EXIT` - Afslutter programmet.