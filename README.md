# 📊 Accounting Online - Backend

## Enterprise ERP Solution - Backend API

**Status:** 🟡 FAZA 0 - Setup
**Tech Stack:** .NET 8.0, ASP.NET Core, Entity Framework Core 8.0

---

## 🏗️ Arhitektura

Clean Architecture pattern:
```
src/
├── ERPAccounting.API/          # Web API Layer
├── ERPAccounting.Application/  # Business Logic
├── ERPAccounting.Domain/        # Domain Models
├── ERPAccounting.Infrastructure/ # Data Access
└── ERPAccounting.Common/        # Shared Utilities
```

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- SQL Server 2019+
- Visual Studio 2022 / VS Code

### Setup
```bash
# Kloniraj repo
git clone https://github.com/ykliugi-beep/accounting-online-backend.git
cd accounting-online-backend

# Build solution
dotnet build

# Run migrations
dotnet ef database update --project src/ERPAccounting.Infrastructure

# Run API
dotnet run --project src/ERPAccounting.API
```

API će biti dostupan na: `https://localhost:5001`

## 📋 Faze Implementacije

### ✅ FAZA 0: PRIPREMA (Tekuća)
- [x] Projektna struktura
- [ ] Dependency setup
- [ ] Database preparacija

### 🔄 FAZA 1: BACKEND - CORE (3 dana)
- [ ] Entity Models & DbContext
- [ ] DTOs & Mappings
- [ ] API Controllers - Lookups

### 🔄 FAZA 2: BACKEND - DOKUMENTI (3 dana)
- [ ] Documents CRUD
- [ ] Line Items CRUD sa ETag (KRITIČNO)
- [ ] Helper endpoints

### 🔄 FAZA 3: BACKEND - TROŠKOVI (1.5 dana)
- [ ] Costs CRUD
- [ ] Cost Distribution Service

---

## 🛠️ Tech Stack

| Komponenta | Verzija |
|------------|--------|
| .NET | 8.0 LTS |
| ASP.NET Core | 8.0 |
| Entity Framework Core | 8.0.0 |
| AutoMapper | 13.0.1 |
| FluentValidation | 11.8.0 |
| Serilog | 7.1.0 |
| xUnit | 2.6.2 |
| Moq | 4.20.70 |

## 📚 Dokumentacija

- [Kompletan Arhitekturni Dokument](docs/arhitektura-kompletna.md)
- [API Specifikacija](docs/json-api-specifikacija.md)
- [Database Model](docs/database-objekti.md)

## 🔐 API Endpoints (Planning)

### Lookup Endpoints
```
GET /api/v1/partners/combo
GET /api/v1/articles/combo
GET /api/v1/tax-rates/combo
```

### Documents
```
POST   /api/v1/documents
GET    /api/v1/documents/{id}
PATCH  /api/v1/documents/{id}
DELETE /api/v1/documents/{id}
```

### Line Items (KRITIČNO - ETag)
```
POST   /api/v1/documents/{id}/items
PATCH  /api/v1/documents/{id}/items/{itemId}  # sa If-Match header
DELETE /api/v1/documents/{id}/items/{itemId}
```

## 📦 Database

**Existing Database:** `Genecom2024Dragicevic`

**Approach:** Database First with EF Core
- Koristi postojeće tabele (tblDokument, tblStavkaDokumenta, itd.)
- Dodaj verzijske tabele za audit trail
- Koristi Stored Procedures za business logic

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

**Target Coverage:** 80%+

## 🎯 Milestone-i

1. **MILESTONE 1:** "Hello World API" (Dan 5)
2. **MILESTONE 2:** "Full CRUD Backend" (Dan 13.5)

---

## 📄 License
MIT

## 👤 Author
ERPAccounting Team
