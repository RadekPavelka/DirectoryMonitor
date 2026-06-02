# Directory Monitor

Webová aplikace pro detekci změn v lokálním adresáři. Aplikace sleduje nové, změněné a smazané soubory a adresáře mezi jednotlivými spuštěními.

## Technologie

- **ASP.NET Core MVC** (.NET 10)
- **Bootstrap 5** (UI framework)
- **SHA256** (detekce změn obsahu souborů)
- **JSON** (persistence dat)
- **xUnit** (unit testy)

## Funkce

1. **Analýza adresáře** - rekurzivní procházení všech souborů a podadresářů
2. **Detekce změn**:
   - Nové soubory
   - Změněné soubory (na základě hash obsahu)
   - Smazané soubory
   - Smazané adresáře
3. **Verzování souborů** - automatické inkrementování verze při změně obsahu
4. **Persistence** - uložení stavu do JSON souboru (bez databáze)

## Jak používat

1. Spusťte aplikaci
2. Zadejte cestu k adresáři (např. `C:\Temp`)
3. Klikněte na tlačítko **Analyzovat**
4. Při prvním spuštění se vytvoří snapshot
5. Při dalších spuštěních se zobrazí detekované změny

## Architektura

```
Controllers/
  └─ HomeController.cs       # MVC controller
Services/
  └─ DirectoryMonitorService.cs  # Business logika
Models/
  ├─ AnalysisResult.cs       # Výsledek analýzy
  ├─ AnalysisResponse.cs     # Response DTO
  ├─ FileMetadata.cs         # Metadata souboru
  └─ DirectorySnapshot.cs    # Snapshot adresáře
Views/
  └─ Home/Index.cshtml       # UI
Data/
  └─ snapshots/*.json        # Persistence (snapshot per monitored path)
Tests/ 
  └─ Services/DirectoryMonitorServiceTests.cs  # Unit testy (6 testů)
```

## Implementační detaily

### Detekce změn obsahu
- Používá **SHA256 hash** pro spolehlivou detekci změn
- Nezávislé na timestamp nebo velikosti souboru

### Verzování
- Nové soubory začínají na verzi 1
- Každá změna obsahu inkrementuje verzi o 1
- Neměněné soubory zachovávají původní verzi

### Persistence
- Data uložena v `Data/snapshots/*.json` (samostatný snapshot pro každou monitorovanou cestu)
- Název snapshot souboru je odvozen z hash normalizované cesty
- Obsahuje relativní cesty (portable napříč stroji)
- JSON formát s odsazením pro čitelnost

### Asynchronní operace
- Všechny I/O operace používají `async/await`
- File operace: `ReadAllTextAsync`, `WriteAllTextAsync`, `ComputeHashAsync`
- Optimální využití thread pool

## Omezení řešení

1. Přejmenování nebo přesun souboru je detekováno jako smazání + vytvoření nového souboru (nelze odlišit od skutečného delete + create)
2. Pokud je soubor během analýzy uzamčen jiným procesem, může analýza skončit chybou.
3. Pro každou monitorovanou cestu se ukládá pouze poslední snapshot (bez historie běhů).

## Error handling

- ✅ `UnauthorizedAccessException` - přístup odepřen
- ✅ `IOException` - chyba při čtení souborů
- ✅ Validace existence adresáře
- ✅ User-friendly chybové hlášky v češtině

## Unit testy
Testují business logiku v `DirectoryMonitorService.Compare()`:
- ✅ Detekce nových souborů
- ✅ Detekce změněných souborů + verzování
- ✅ Zachování verzí u neměněných souborů
- ✅ Detekce smazaných souborů
- ✅ Detekce smazaných adresářů
- ✅ Komplexní mix změn

---

**Autor:** Radek Pavelka  
**Datum:** 2. 6. 2026  
**Framework:** .NET 10 / ASP.NET Core MVC