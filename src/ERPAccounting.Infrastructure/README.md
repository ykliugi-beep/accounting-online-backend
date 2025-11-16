# ERPAccounting.Infrastructure

**Data Access Layer**

## Odgovornosti
- Database context (EF Core)
- Repository pattern
- Migrations
- Database seeding
- External services integration

## Struktura
```
Data/
├── AppDbContext.cs
├── DbContextOptionsBuilder.cs
├── Migrations/
└── Seeds/

Repositories/
├── DocumentRepository.cs
├── DocumentItemRepository.cs
├── UnitOfWork.cs
└── IRepository.cs

Specifications/
└── SpecificationEvaluator.cs
```

## Database Connection

**Existing Database:** `Genecom2024Dragicevic`

**Approach:** Database First

```bash
# Scaffold existing tables
dotnet ef dbcontext scaffold "Server=...;Database=Genecom2024Dragicevic;..." \
  Microsoft.EntityFrameworkCore.SqlServer \
  --output-dir Data/Models
```

## Dependencies
- ERPAccounting.Domain
- Entity Framework Core 8.0
- SQL Server Provider
