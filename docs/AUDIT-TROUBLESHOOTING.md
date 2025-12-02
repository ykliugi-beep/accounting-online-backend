# Audit Sistem - Troubleshooting Guide

## 🔴 Trenutni Problemi

### Problem 1: ResponseBody je NULL za uspešne operacije

**Simptomi:**
- ✅ `tblAPIAuditLog` - red se upisuje
- ❌ `ResponseBody` kolona je NULL
- ✅ Za error responses (400) `ResponseBody` je popunjen

**Uzrok:**
Stari kod je hvatao samo error responses:

```csharp
// STARO - hvatalo samo errore
if (auditLog.IsSuccess == false)
{
    auditLog.ResponseBody = await reader.ReadToEndAsync();
}
```

**Rešenje:**
✅ **ISPRAVLJENA** verzija hvata SVE responses za POST/PUT/DELETE:

```csharp
// NOVO - hvata SVE responses
if (request.Method == "POST" || request.Method == "PUT" || request.Method == "DELETE")
{
    auditLog.ResponseBody = await reader.ReadToEndAsync();
}
```

---

### Problem 2: Podaci nisu promenjeni u bazi

**Simptomi:**
- API vraća success response
- Podaci ostaju isti u bazi (nisu update-ovani)

**Mogući uzroci:**

#### A) SaveChanges se NE poziva

**Provera:**
```sql
-- Proveri da li postoji bilo kakva aktivnost
SELECT TOP 10 *
FROM tblAPIAuditLog
WHERE HttpMethod IN ('POST', 'PUT', 'DELETE')
ORDER BY Timestamp DESC;
```

Ako je `ResponseStatusCode = 200/201` ali podaci nisu promenjeni, znači:
- Controller je izvršen uspešno
- Service je vraćao DTO
- **ALI `SaveChangesAsync()` nije pozvan ili je failovao tiho**

#### B) Exception se desi PRE SaveChanges

Proveri:
```csharp
// U DocumentService.CreateDocumentAsync:
await _documentRepository.AddAsync(entity);
await _unitOfWork.SaveChangesAsync(); // <-- Da li se izvršava?
```

Ako exception nastane PRE `SaveChangesAsync`, middleware će uhvatiti exception ali promene neće biti commitovane.

#### C) Transaction rollback

Ako je `SaveChangesAsync` unutar transakcije koja se rollback-uje, promene neće biti perzistovane.

---

### Problem 3: tblAPIAuditLogEntityChanges prazna

**Simptomi:**
- ✅ `tblAPIAuditLog` ima redove
- ❌ `tblAPIAuditLogEntityChanges` je prazna
- Snapshots se ne zapisuju

**Mogući uzroci:**

#### A) HttpContext.Items audit log ID nije setovan

**Dijagnostika:**

Dodaj breakpoint u `AppDbContext.SaveChangesAsync`:

```csharp
if (_httpContextAccessor?.HttpContext != null)
{
    if (_httpContextAccessor.HttpContext.Items.TryGetValue("__AuditLogId__", out var auditLogIdObj))
    {
        // BREAKPOINT OVDE - da li se izvršava?
        if (auditLogIdObj is int auditLogId)
        {
            currentAuditLogId = auditLogId;
            // Da li je currentAuditLogId setuvan?
        }
    }
}
```

**Provera kroz log:**

Ako je logging level na `Debug`, trebalo bi da vidiš:
```
[DBG] Audit log ID 1234 set in HttpContext for POST /api/v1/documents
```

Ako ovo NIJE u logu, problem je u middleware-u.

#### B) ChangeTracker je prazan

**Uzrok:** `SaveChangesAsync` se poziva, ali ChangeTracker nema tracked entities.

**Provera:**

Dodaj log u `CaptureEntitySnapshots`:

```csharp
var entries = ChangeTracker.Entries()
    .Where(e => e.State == EntityState.Added || 
               e.State == EntityState.Modified || 
               e.State == EntityState.Deleted)
    .ToList();

_logger?.LogDebug("Found {Count} changed entities in ChangeTracker", entries.Count);
// Ako je Count = 0, onda nema šta da se loguje!
```

**Razlozi za prazan ChangeTracker:**

1. **Entiteti nisu track-ovani**
   ```csharp
   // LOŠE - ne trackuje se
   var doc = new Document { ... };
   context.Entry(doc).State = EntityState.Detached; // Problem!
   
   // DOBRO - trackuje se
   context.Documents.Add(doc);
   ```

2. **AsNoTracking u repository-ju**
   ```csharp
   // Ako repository koristi:
   return await _context.Documents
       .AsNoTracking() // <-- Problem!
       .FirstOrDefaultAsync();
   ```

3. **SaveChanges pozvan na DRUGOM context-u**
   ```csharp
   var doc = new Document();
   context1.Documents.Add(doc);
   await context2.SaveChangesAsync(); // <-- Neće pronaći promene!
   ```

#### C) AuditLogService je NULL

**Provera:**

```csharp
if (_auditLogService == null)
{
    _logger?.LogWarning("AuditLogService is null, cannot log snapshots");
    return;
}
```

Ako je `IAuditLogService` injektovan kao `null`, snapshots neće biti logovani.

#### D) Exception u LogEntitySnapshotAsync

**Provera kroz SQL:**

```sql
-- Proveri da li postoje orphan audit logs (bez entity changes)
SELECT 
    al.IDAuditLog,
    al.OperationType,
    al.EntityType,
    al.EntityId,
    COUNT(ec.IDEntityChange) AS ChangeCount
FROM tblAPIAuditLog al
LEFT JOIN tblAPIAuditLogEntityChanges ec ON al.IDAuditLog = ec.IDAuditLog
WHERE al.OperationType IN ('Insert', 'Update', 'Delete')
GROUP BY al.IDAuditLog, al.OperationType, al.EntityType, al.EntityId
HAVING COUNT(ec.IDEntityChange) = 0
ORDER BY al.Timestamp DESC;
```

Ako ima mnogo ovakvih, znači da `LogEntitySnapshotAsync` faila tiho.

---

## 🔍 Dijagnostički Query-ji

### 1. Proveri da li middleware radi

```sql
-- Sve API pozive u poslednjih 10 minuta
SELECT 
    IDAuditLog,
    Timestamp,
    HttpMethod,
    Endpoint,
    ResponseStatusCode,
    IsSuccess,
    CASE 
        WHEN ResponseBody IS NULL THEN 'NULL'
        ELSE 'POPULATED'
    END AS ResponseBodyStatus
FROM tblAPIAuditLog
WHERE Timestamp > DATEADD(MINUTE, -10, GETUTCDATE())
ORDER BY Timestamp DESC;
```

### 2. Proveri entity changes

```sql
-- Proveri da li ima IKAKVIH entity changes
SELECT COUNT(*)
FROM tblAPIAuditLogEntityChanges;

-- Ako je 0, problem je u SaveChangesAsync override-u
```

### 3. Proveri orphan audit logs

```sql
-- Audit logs bez entity changes
SELECT 
    al.*,
    COUNT(ec.IDEntityChange) AS EntityChangeCount
FROM tblAPIAuditLog al
LEFT JOIN tblAPIAuditLogEntityChanges ec ON al.IDAuditLog = ec.IDAuditLog
WHERE al.OperationType IN ('Insert', 'Update', 'Delete')
GROUP BY al.IDAuditLog, al.Timestamp, al.HttpMethod, al.Endpoint, 
         al.OperationType, al.ResponseStatusCode, al.IsSuccess,
         al.Username, al.EntityType, al.EntityId, al.RequestBody,
         al.ResponseBody, al.ErrorMessage, al.ExceptionDetails,
         al.ResponseTimeMs, al.UserId, al.RequestPath, al.QueryString,
         al.IPAddress, al.UserAgent, al.CorrelationId
HAVING COUNT(ec.IDEntityChange) = 0;
```

### 4. Proveri da li se podaci menjaju u bazi

```sql
-- Test: Pokušaj da update-uješ dokument
UPDATE tblDokument
SET BrojDokumenta = 'TEST-' + BrojDokumenta
WHERE IDDokument = 259602;

-- Proveri da li je promenjen
SELECT BrojDokumenta FROM tblDokument WHERE IDDokument = 259602;

-- Rollback
ROLLBACK;
```

Ako ovaj direktni UPDATE ne radi, problem je na bazi (permissions, locks, triggers).

---

## 🚪️ Provera Toka Podataka

### Korak po Korak Debugging

#### 1. Proveri Middleware

**Dodaj log u ApiAuditMiddleware POSLE postavljanja ID-a:**

```csharp
if (auditLogId > 0)
{
    context.Items[AuditLogIdKey] = auditLogId;
    
    _logger.LogInformation(
        "[AUDIT-DEBUG] Audit log ID {AuditLogId} set in HttpContext for {Method} {Endpoint}",
        auditLogId,
        request.Method,
        request.Path);
}
```

**Očekivani log:**
```
[INF] [AUDIT-DEBUG] Audit log ID 1234 set in HttpContext for POST /api/v1/documents
```

#### 2. Proveri AppDbContext

**Dodaj log u SaveChangesAsync:**

```csharp
if (_httpContextAccessor?.HttpContext != null)
{
    if (_httpContextAccessor.HttpContext.Items.TryGetValue("__AuditLogId__", out var auditLogIdObj))
    {
        if (auditLogIdObj is int auditLogId)
        {
            currentAuditLogId = auditLogId;
            _logger?.LogInformation(
                "[AUDIT-DEBUG] Retrieved audit log ID {AuditLogId} from HttpContext",
                auditLogId);
        }
    }
    else
    {
        _logger?.LogWarning("[AUDIT-DEBUG] No audit log ID found in HttpContext.Items");
    }
}
else
{
    _logger?.LogWarning("[AUDIT-DEBUG] HttpContext is null in DbContext");
}
```

**Očekivani log:**
```
[INF] [AUDIT-DEBUG] Retrieved audit log ID 1234 from HttpContext
[DBG] Found 1 changed entities in ChangeTracker
[DBG] Captured INSERT snapshot for Document:259602
[DBG] Successfully logged snapshot for Document:259602 to audit log 1234
```

#### 3. Proveri da li postoje promene u ChangeTracker

**Pre `base.SaveChangesAsync()`, dodaj:**

```csharp
var changedEntries = ChangeTracker.Entries()
    .Where(e => e.State != EntityState.Unchanged && e.State != EntityState.Detached)
    .ToList();

_logger?.LogInformation(
    "[AUDIT-DEBUG] SaveChangesAsync called with {Count} changed entities",
    changedEntries.Count);

foreach (var entry in changedEntries)
{
    _logger?.LogDebug(
        "[AUDIT-DEBUG] Entity: {EntityType}, State: {State}, PK: {PrimaryKey}",
        entry.Entity.GetType().Name,
        entry.State,
        GetPrimaryKeyValue(entry));
}
```

**Očekivani log:**
```
[INF] [AUDIT-DEBUG] SaveChangesAsync called with 1 changed entities
[DBG] [AUDIT-DEBUG] Entity: Document, State: Added, PK: 0
```

Ako je `Count = 0`, znači da ChangeTracker nema promene!

---

## 🧪 Test Scenario

### Kreiranje Dokumenta - Očekivani Tok

**1. API Request:**
```http
POST /api/v1/documents
Content-Type: application/json

{
  "brojDokumenta": "TEST-001",
  "idVrstaDokumenta": 1,
  "idPartner": 123
}
```

**2. Middleware:

```
✅ Kreira ApiAuditLog zapis
✅ Dobija IDAuditLog = 1234
✅ Postavlja context.Items["__AuditLogId__"] = 1234
[LOG] Audit log ID 1234 set in HttpContext for POST /api/v1/documents
```

**3. Controller → Service → UnitOfWork:**

```
✅ DocumentController.CreateDocument
✅ DocumentService.CreateDocumentAsync
✅ _documentRepository.AddAsync(entity)
✅ _unitOfWork.SaveChangesAsync() // <-- Poziva AppDbContext.SaveChangesAsync
```

**4. AppDbContext.SaveChangesAsync:**

```
✅ Čita audit log ID iz HttpContext.Items
[LOG] Retrieved audit log ID 1234 from HttpContext

✅ Prikuplja snapshots iz ChangeTracker
[LOG] Found 1 changed entities in ChangeTracker
[LOG] Entity: Document, State: Added, PK: 0
[LOG] Captured INSERT snapshot for Document:0

✅ Izvršava base.SaveChangesAsync() - upisuje u bazu

✅ Loguje snapshots
[LOG] Successfully logged snapshot for Document:259602 to audit log 1234
```

**5. Rezultat u bazi:**

```sql
-- tblDokument
INSERT INTO tblDokument (BrojDokumenta, IDVrstaDokumenta, IDPartner, ...)
VALUES ('TEST-001', 1, 123, ...)
-- IDDokument = 259602 (auto-generated)

-- tblAPIAuditLog
IDAuditLog = 1234
OperationType = 'Insert'
EntityType = 'Document'
EntityId = '259602'
ResponseBody = '{"id": 259602, "brojDokumenta": "TEST-001", ...}'

-- tblAPIAuditLogEntityChanges
IDEntityChange = 5001
IDAuditLog = 1234
PropertyName = '__FULL_SNAPSHOT__'
OldValue = NULL
NewValue = '{"idDokument": 259602, "brojDokumenta": "TEST-001", ...}'
```

---

## ⚙️ Privremeni Debug Logging

### Kako Dodati Detaljno Logovanje

**1. U appsettings.Development.json:**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "ERPAccounting.Infrastructure.Middleware": "Debug",
      "ERPAccounting.Infrastructure.Data": "Debug",
      "ERPAccounting.Infrastructure.Services": "Debug",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

**2. Enable SQL Query Logging (privremeno):**

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

Ovo će logovati SVE SQL query-je koji se izvršavaju.

### Očekivani Logovi

**Za uspešan CREATE:**

```
[DBG] Audit log ID 1234 set in HttpContext for POST /api/v1/documents
[DBG] Retrieved audit log ID 1234 from HttpContext
[DBG] SaveChangesAsync called with 1 changed entities
[DBG] Entity: Document, State: Added, PK: 0
[DBG] Found 1 changed entities in ChangeTracker
[DBG] Captured INSERT snapshot for Document:0
[INF] Executing SQL: INSERT INTO tblDokument (...)
[DBG] Successfully logged snapshot for Document:259602 to audit log 1234
[INF] Logged JSON snapshot for Document 259602 (Operation: Added)
```

**Ako nešto nedostaje, znamo gde je problem!**

---

## 🔧 Brze Ispravke

### Ispravka 1: Response Body Capture (✅ VEĆ URAĐENO)

```csharp
// U ApiAuditMiddleware.cs, linija ~95
if (request.Method == "POST" || request.Method == "PUT" || request.Method == "DELETE")
{
    auditLog.ResponseBody = await reader.ReadToEndAsync();
}
```

### Ispravka 2: HttpContext.Items Pristup (✅ VEĆ URAĐENO)

```csharp
// U ApiAuditMiddleware.cs
context.Items["__AuditLogId__"] = auditLogId;

// U AppDbContext.cs
if (_httpContextAccessor?.HttpContext?.Items.TryGetValue("__AuditLogId__", out var auditLogIdObj) == true)
{
    currentAuditLogId = auditLogIdObj as int?;
}
```

### Ispravka 3: Registracija HttpContextAccessor (✅ VEĆ URAĐENO)

```csharp
// U ServiceCollectionExtensions.cs
services.AddHttpContextAccessor();
```

---

## 🚨 Najčešći Problemi

### Problem: "Podaci nisu promenjeni"

**Checklist:**

- [ ] Da li `SaveChangesAsync()` baca exception? (proveri try-catch)
- [ ] Da li je response status 200/201? (ako je 400/500, save nije izvršen)
- [ ] Da li repository koristi `AsNoTracking()`? (proverite GetByIdAsync)
- [ ] Da li se poziva na pravom context-u? (jedan context instance)
- [ ] Da li entity ima `[NotMapped]` properties koje se pokušavaju da sačuvaju?

### Problem: "Entity changes nisu logovani"

**Checklist:**

- [ ] Da li je `IHttpContextAccessor` registrovan? (`services.AddHttpContextAccessor()`)
- [ ] Da li `AppDbContext` prima `IHttpContextAccessor` u konstruktoru?
- [ ] Da li middleware postavlja `context.Items["__AuditLogId__"]`?
- [ ] Da li `SaveChangesAsync` čita iz `HttpContext.Items`?
- [ ] Da li ChangeTracker ima tracked entities?
- [ ] Da li `IAuditLogService` injektovan u `AppDbContext`?

### Problem: "ResponseBody je NULL"

**Checklist:**

- [ ] Da li middleware čita response stream?
- [ ] Da li je `responseBodyStream` seek-ovan na početak?
- [ ] Da li je filter samo za error responses? (treba za sve POST/PUT/DELETE)
- [ ] Da li je `responseBodyStream.Length > 0`?

---

## 🐛 Debugging Commands

### Enable Detailed Logging

```bash
# U appsettings.Development.json
"Microsoft.EntityFrameworkCore.Database.Command": "Information"
```

### Watch SQL Activity

```sql
-- Real-time monitoring
SELECT 
    session_id,
    start_time,
    status,
    command,
    database_name,
    wait_type,
    wait_time,
    last_wait_type,
    text
FROM sys.dm_exec_requests
CROSS APPLY sys.dm_exec_sql_text(sql_handle)
WHERE database_name = 'ERPAccounting_Tmp'
ORDER BY start_time DESC;
```

### Check Triggers

```sql
-- Proveri da li triggeri postoje
SELECT 
    t.name AS TriggerName,
    OBJECT_NAME(t.parent_id) AS TableName,
    t.is_disabled
FROM sys.triggers t
WHERE OBJECT_NAME(t.parent_id) IN ('tblDokument', 'tblStavkaDokumenta')
```

---

## ✅ Test Plan

### Manual Test

1. **Postavi logging na Debug**
2. **Pozovi POST /api/v1/documents**
3. **Proveri logove:**
   - Da li se pojavljuje "Audit log ID {X} set in HttpContext"?
   - Da li se pojavljuje "Retrieved audit log ID {X} from HttpContext"?
   - Da li se pojavljuje "Found {N} changed entities in ChangeTracker"?
   - Da li se pojavljuje "Successfully logged snapshot"?

4. **Proveri bazu:**
   ```sql
   -- Poslednji audit log
   SELECT TOP 1 * FROM tblAPIAuditLog ORDER BY Timestamp DESC;
   
   -- Entity changes za taj audit log
   SELECT * FROM tblAPIAuditLogEntityChanges 
   WHERE IDAuditLog = (SELECT TOP 1 IDAuditLog FROM tblAPIAuditLog ORDER BY Timestamp DESC);
   ```

5. **Proveri da li su podaci update-ovani:**
   ```sql
   SELECT TOP 1 * FROM tblDokument ORDER BY IDDokument DESC;
   ```

---

## 📞 Kontakt / Pomoć

Ako problem i dalje postoji:

1. **Pošalji logove** sa Debug level-om
2. **Pošalji rezultate SQL query-ja** gore
3. **Pošalji request/response** iz Postman/Swagger-a

Sa ovim podacima mogu precizno da identifikujem problem!