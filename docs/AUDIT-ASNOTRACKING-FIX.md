# AsNoTracking Bug - API Returns 200 But Database Unchanged

**Datum:** 27. Novembar 2025, 22:40 CET  
**Status:** ✅ **RESOLVED**  
**Commits:** 81960ba, 5231fab

---

## 🔴 Problem

### Simptomi

```
1. PUT /api/v1/documents/259602/costs/116373
   Body: { "datumDPO": "2024-11-27", ... }

2. API Response: HTTP 200 OK ✅
   Body: { "id": 116373, "datumDPO": "2024-11-27", ... }

3. tblAPIAuditLog: ✅
   - RequestBody: POPULATED
   - ResponseBody: POPULATED
   - ResponseStatusCode: 200

4. tblAPIAuditLogEntityChanges: ❌
   - 0 redova (prazan!)

5. Database Check:
   SELECT DatumDPO FROM tblDokumentTroskovi WHERE IDDokumentTroskovi = 116373
   Result: NULL ❌ (nije promenjen!)
```

**Pitanje:** Kako API može da vrati "success" response sa novim podacima, a database ostaje nepromenjen?

---

## 🔍 Root Cause Analysis

### Problem #1: Service Ne Prosleđuje `track` Parametar

**Lokacija:** `DocumentCostService.cs` - `EnsureCostExistsAsync` metoda

**STARI KOD:**

```csharp
private async Task<DocumentCost> EnsureCostExistsAsync(int documentId, int costId, bool track = false)
{
    // ❌ track parametar se IGNORIŠE!
    var entity = await _costRepository.GetAsync(documentId, costId, includeChildren: true);
    //                                                                 ^
    //                                                                 track parametar nije prosleđen!
    
    if (entity is null)
    {
        throw new NotFoundException(ErrorMessages.DocumentCostNotFound, costId.ToString(), nameof(DocumentCost));
    }

    return entity;
}
```

**Posledica:**
- Service poziva `EnsureCostExistsAsync(documentId, costId, track: true)`
- Ali repository dobija `track = false` (default vrednost)
- Entity se učitava sa `AsNoTracking()`

---

### Problem #2: Repository Pogrešna Logika za `track` Parametar

**Lokacija:** `DocumentCostRepository.cs` - `GetAsync` metoda

**STARI KOD:**

```csharp
public async Task<DocumentCost?> GetAsync(
    int documentId,
    int costId,
    bool track = false,
    bool includeChildren = false,
    CancellationToken cancellationToken = default)
{
    IQueryable<DocumentCost> query = _context.DocumentCosts
        .Where(cost => cost.IDDokumentTroskovi == costId && cost.IDDokument == documentId);

    // ❌ PROBLEM: AsNoTracking() se dodaje AKO je includeChildren=true!
    if (includeChildren && !track)
    {
        query = query
            .Include(cost => cost.CostLineItems)
                .ThenInclude(item => item.VATItems)
            .AsNoTracking();  // ❌ Dodaje AsNoTracking!
    }

    // ❌ PROBLEM: Ponovo dodaje AsNoTracking() jer je track=false
    query = track ? query.AsTracking() : query.AsNoTracking();

    return await query
        .Where(cost => cost.IDDokumentTroskovi == costId && cost.IDDokument == documentId)
        .FirstOrDefaultAsync(cancellationToken);
}
```

**Posledica:**
- Service poziva: `GetAsync(documentId, costId, includeChildren: true)` (bez track parametra)
- Repository vidi: `track = false` (default), `includeChildren = true`
- Ulazi u `if (includeChildren && !track)` blok
- Dodaje `.AsNoTracking()` prvi put
- Zatim izvršava: `query = track ? query.AsTracking() : query.AsNoTracking();`
- Dodaje `.AsNoTracking()` drugi put
- **Entity se učitava bez tracking-a!**

---

## 🔄 Tok Podataka (Pre Fix-a)

```
1. Controller poziva:
   UpdateCostAsync(documentId, costId, expectedRowVersion, dto)

2. Service (UpdateCostAsync):
   var entity = await EnsureCostExistsAsync(documentId, costId, track: true);
   //                                                                ^
   //                                                                Service traži tracking

3. EnsureCostExistsAsync:
   var entity = await _costRepository.GetAsync(documentId, costId, includeChildren: true);
   //                                                                ^
   //                                                                ❌ track parametar NIJE prosleđen!

4. Repository.GetAsync:
   Dobija: track = false (default vrednost)
   Primenjuje: .AsNoTracking()
   //          ^
   //          Entity se učitava BEZ tracking-a!

5. Service nastavlja:
   entity.DatumDPO = dto.DueDate;  // Menja se in-memory objekat
   entity.DatumValute = dto.CurrencyDate;
   ...

6. SaveChangesAsync:
   ChangeTracker.Entries() = prazan (entity nije trackan!)
   //                        ^
   //                        Nema detektovanih promena!
   return 0;  // ❌ 0 changes saved

7. API Response:
   var dto = MapToDto(entity);  // DTO se kreira sa in-memory podacima
   return Ok(dto);  // ✅ Vraća 200 OK sa "novim" podacima
   //                   ALI - podaci su samo in-memory, nisu u bazi!

8. Database:
   SELECT DatumDPO FROM tblDokumentTroskovi WHERE IDDokumentTroskovi = 116373;
   Result: NULL  // ❌ Nije promenjen!
```

**Zašto API vraća "success" response?**

API kreira DTO iz **in-memory objekta** sa izmenjenim poljima. Database ostaje nepromenjen jer EF Change Tracker nije video nikakve promene.

---

## ✅ Rešenje

### Fix #1: Service Prosleđuje `track` Parametar (Commit 81960ba)

**NOVI KOD:**

```csharp
/// <summary>
/// KRITIČNA ISPRAVKA: Prosleđuje track parametar u repository!
/// </summary>
private async Task<DocumentCost> EnsureCostExistsAsync(int documentId, int costId, bool track = false)
{
    // ✅ FIX: Prosleđuje track parametar!
    var entity = await _costRepository.GetAsync(
        documentId, 
        costId, 
        track: track,                    // ✅ Prosleđen!
        includeChildren: !track);        // ✅ includeChildren samo ako je track=false
    
    if (entity is null)
    {
        throw new NotFoundException(ErrorMessages.DocumentCostNotFound, costId.ToString(), nameof(DocumentCost));
    }

    return entity;
}
```

**Zašto `includeChildren: !track`?**

Kada je `track=true`, tražimo samo header entity za update. Nećemo menjati child entities (CostLineItems, VATItems), pa nam ne trebaju učitani.

---

### Fix #2: Repository Pravilna Logika za `track` (Commit 5231fab)

**NOVI KOD:**

```csharp
/// <summary>
/// KRITIČNA ISPRAVKA: Pravilna primena track parametra.
/// </summary>
public async Task<DocumentCost?> GetAsync(
    int documentId,
    int costId,
    bool track = false,
    bool includeChildren = false,
    CancellationToken cancellationToken = default)
{
    IQueryable<DocumentCost> query = _context.DocumentCosts
        .Where(cost => cost.IDDokumentTroskovi == costId && cost.IDDokument == documentId);

    // ✅ FIX: Include children nezavisno od track parametra
    if (includeChildren)
    {
        query = query
            .Include(cost => cost.CostLineItems)
                .ThenInclude(item => item.VATItems);
    }

    // ✅ FIX: Primeni tracking JEDNOM na osnovu track parametra
    query = track ? query : query.AsNoTracking();
    //      ^
    //      Ako je track=true, NE dodaje se AsNoTracking()
    //      AsTracking() je default, ne mora se eksplicitno pozivati

    return await query.FirstOrDefaultAsync(cancellationToken);
}
```

**Ključne promene:**
1. ✅ `includeChildren` blok više ne dodaje `.AsNoTracking()`
2. ✅ Tracking se primenjuje JEDNOM na osnovu `track` parametra
3. ✅ Kada je `track=true`, query ostaje sa default tracking-om (AsTracking)

---

## 🔄 Tok Podataka (Posle Fix-a)

```
1. Controller poziva:
   UpdateCostAsync(documentId, costId, expectedRowVersion, dto)

2. Service (UpdateCostAsync):
   var entity = await EnsureCostExistsAsync(documentId, costId, track: true);

3. EnsureCostExistsAsync:
   var entity = await _costRepository.GetAsync(
       documentId, 
       costId, 
       track: true,           // ✅ Prosleđen!
       includeChildren: false // ✅ Ne trebaju child entities za update
   );

4. Repository.GetAsync:
   Dobija: track = true
   Primenjuje: query ostaje sa default AsTracking()
   //          ^
   //          ✅ Entity se učitava SA tracking-om!

5. Service nastavlja:
   entity.DatumDPO = dto.DueDate;  // Menja trackovani objekat
   entity.DatumValute = dto.CurrencyDate;
   ...

6. SaveChangesAsync:
   ChangeTracker.Entries() = [ Modified: entity ]
   //                        ^
   //                        ✅ Promene detektovane!
   
   // AppDbContext.SaveChangesAsync izvršava:
   - Hvata ChangeTracker.Entries()
   - Kreira JSON snapshots (OldValue/NewValue)
   - Loguje u tblAPIAuditLogEntityChanges
   - Izvršava UPDATE u bazi
   
   return 1;  // ✅ 1 entity updated

7. API Response:
   var dto = MapToDto(entity);  // DTO sa stvarno update-ovanim podacima
   return Ok(dto);  // ✅ Vraća 200 OK

8. Database:
   SELECT DatumDPO FROM tblDokumentTroskovi WHERE IDDokumentTroskovi = 116373;
   Result: '2024-11-27'  // ✅ Promenjen!

9. tblAPIAuditLogEntityChanges:
   SELECT * FROM tblAPIAuditLogEntityChanges WHERE IDAuditLog = {audit_log_id};
   Result: 1 red sa OldValue/NewValue JSON-om  // ✅ Snapshot logovan!
```

---

## 🧪 Verifikacija

### Test 1: PUT Request - Update Existing Cost

**Request:**
```http
PUT /api/v1/documents/259602/costs/116373
If-Match: "{etag}"
Content-Type: application/json

{
  "partnerId": 123,
  "documentTypeCode": "EV",
  "documentNumber": "B2/11/24",
  "dueDate": "2024-11-27",
  "currencyDate": "2024-11-28",
  "description": "Test update",
  "statusId": 1,
  "currencyId": 47,
  "exchangeRate": 117.2299
}
```

**Očekivano:**

```sql
-- 1. Database update
SELECT 
    DatumDPO, 
    DatumValute, 
    Opis 
FROM tblDokumentTroskovi 
WHERE IDDokumentTroskovi = 116373;

-- Result:
-- DatumDPO: 2024-11-27 ✅
-- DatumValute: 2024-11-28 ✅
-- Opis: 'Test update' ✅

-- 2. Audit log
SELECT 
    RequestBody,
    ResponseBody,
    ResponseStatusCode
FROM tblAPIAuditLog
WHERE HttpMethod = 'PUT'
    AND Endpoint LIKE '%/costs/116373'
ORDER BY Timestamp DESC;

-- Result:
-- RequestBody: '{...}' ✅
-- ResponseBody: '{...}' ✅
-- ResponseStatusCode: 200 ✅

-- 3. Entity snapshot
SELECT 
    PropertyName,
    LEFT(OldValue, 100) AS OldValuePreview,
    LEFT(NewValue, 100) AS NewValuePreview
FROM tblAPIAuditLogEntityChanges ec
INNER JOIN tblAPIAuditLog al ON ec.IDAuditLog = al.IDAuditLog
WHERE al.HttpMethod = 'PUT'
    AND al.Endpoint LIKE '%/costs/116373'
ORDER BY ec.IDEntityChange DESC;

-- Result:
-- PropertyName: '__FULL_SNAPSHOT__'
-- OldValuePreview: '{"datumDPO":null,"datumValute":"2024-01-09","opis":null,...' ✅
-- NewValuePreview: '{"datumDPO":"2024-11-27","datumValute":"2024-11-28","opis":"Test update",...' ✅
```

---

### Test 2: Provera ChangeTracker State-a

Dodaj privremeni log u `DocumentCostService.UpdateCostAsync`:

```csharp
public async Task<DocumentCostDto> UpdateCostAsync(...)
{
    await ValidateAsync(_updateCostValidator, dto);

    var entity = await EnsureCostExistsAsync(documentId, costId, track: true);

    // PRIVREMENI DEBUG LOG
    var state = _unitOfWork.Context.Entry(entity).State;  // Dodaj IUnitOfWork.Context property
    _logger.LogInformation(
        "[DEBUG] Entity state after load: {State}, IsTracked: {IsTracked}",
        state,
        state != EntityState.Detached);

    EnsureRowVersion(entity.DokumentTroskoviTimeStamp, expectedRowVersion, costId, nameof(DocumentCost));
    
    // ... update fields ...

    // PRIVREMENI DEBUG LOG
    var stateAfterUpdate = _unitOfWork.Context.Entry(entity).State;
    _logger.LogInformation(
        "[DEBUG] Entity state after update: {State}",
        stateAfterUpdate);

    await _unitOfWork.SaveChangesAsync();

    return MapToDto(entity);
}
```

**Očekivani log:**
```
[INF] [DEBUG] Entity state after load: Unchanged, IsTracked: True
[INF] [DEBUG] Entity state after update: Modified
[INF] SaveChangesAsync completed, 1 entities saved
```

---

## 📊 Before/After Comparison

| Aspekt | Pre Fix-a | Posle Fix-a |
|--------|-----------|-------------|
| **track parametar prosleđen?** | ❌ Ne | ✅ Da |
| **Entity tracking** | AsNoTracking() | AsTracking() |
| **ChangeTracker.Entries()** | Prazan | 1 Modified entity |
| **SaveChangesAsync result** | 0 | 1 |
| **Database UPDATE izvršen?** | ❌ Ne | ✅ Da |
| **tblAPIAuditLogEntityChanges** | Prazan | 1 snapshot |
| **API Response** | 200 OK (lažni podaci) | 200 OK (stvarni podaci) |
| **Konzistentnost** | API != Database | API == Database |

---

## 💡 Key Lessons

### 1. AsNoTracking() Ignoriše Sve Promene

```csharp
// Loše
var entity = await _context.Entities.AsNoTracking().FirstOrDefaultAsync(...);
entity.SomeField = newValue;
await _context.SaveChangesAsync();  // ❌ 0 changes

// Dobro
var entity = await _context.Entities.FirstOrDefaultAsync(...);
entity.SomeField = newValue;
await _context.SaveChangesAsync();  // ✅ 1 change
```

### 2. Default Parametri Mogu Biti Opasni

```csharp
// Opasno
private async Task<Entity> LoadEntity(int id, bool track = false)
{
    return await _repo.GetAsync(id, track: track);
    //                                        ^
    //                                        Ako pozivač ne prosleđuje track,
    //                                        koristi se default (false)!
}

// Sigurnije
private async Task<Entity> LoadEntity(int id, bool track)
{
    return await _repo.GetAsync(id, track: track);
    //                                        ^
    //                                        Compiler error ako track nije prosleđen!
}
```

### 3. Repository Pattern - Track Parametar Je Obavezan

Svaki repository koji vraća entitete za UPDATE MORA imati `track` parametar:

```csharp
public interface IRepository<T>
{
    Task<T?> GetAsync(int id, bool track = false);  // ✅ Track parametar prisutan
    Task<IReadOnlyList<T>> GetAllAsync(bool track = false);
}
```

### 4. Verifikuj Entity State Pre SaveChanges

Za kritične operacije, proveri state:

```csharp
var state = _context.Entry(entity).State;
if (state == EntityState.Detached)
{
    throw new InvalidOperationException("Entity is not tracked!");
}
```

---

## ✅ Summary

**Problem:** API vraćao "success" response, ali database ostao nepromenjen.

**Root Cause:** 
1. Service nije prosleđivao `track` parametar u repository
2. Repository imao pogrešnu logiku koja je primenjivala `AsNoTracking()` više puta

**Rešenje:**
1. ✅ Service sada prosleđuje `track: true` kada traži entity za update
2. ✅ Repository primenjuje tracking JEDNOM na osnovu parametra
3. ✅ Entity se učitava sa tracking-om
4. ✅ Promene se detektuju i čuvaju u bazi
5. ✅ Snapshots se loguju u audit tabelu

**Status:** ✅ **RESOLVED** - commits 81960ba, 5231fab

---

**Vidi takođe:**
- [AUDIT-EF-CHANGE-TRACKER-FIX.md](./AUDIT-EF-CHANGE-TRACKER-FIX.md) - ResponseBody NULL problem
- [AUDIT-TESTING-GUIDE.md](./AUDIT-TESTING-GUIDE.md) - Kompletni test plan
- [AUDIT-TROUBLESHOOTING.md](./AUDIT-TROUBLESHOOTING.md) - Debugging guide
