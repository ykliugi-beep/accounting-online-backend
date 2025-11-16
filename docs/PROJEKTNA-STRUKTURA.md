# 🏗️ Projektna Struktura - Backend

**Datum:** 16.11.2025
**Faza:** FAZA 0.1 - Checkpoint

---

## Clean Architecture Layers

```
accounting-online-backend/
├── src/
│   ├── ERPAccounting.API/
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── ERPAccounting.API.csproj
│   │
│   ├── ERPAccounting.Application/
│   │   ├── Contracts/
│   │   ├── DTOs/
│   │   ├── Services/
│   │   ├── Mapping/
│   │   ├── Validators/
│   │   └── ERPAccounting.Application.csproj
│   │
│   ├── ERPAccounting.Domain/
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   ├── Specifications/
│   │   └── ERPAccounting.Domain.csproj
│   │
│   ├── ERPAccounting.Infrastructure/
│   │   ├── Data/
│   │   ├── Repositories/
│   │   ├── Specifications/
│   │   └── ERPAccounting.Infrastructure.csproj
│   │
│   └── ERPAccounting.Common/
│       ├── Exceptions/
│       ├── Constants/
│       ├── Extensions/
│       ├── Models/
│       └── ERPAccounting.Common.csproj
│
├── tests/
│   ├── ERPAccounting.API.Tests/
│   ├── ERPAccounting.Application.Tests/
│   └── ERPAccounting.Integration.Tests/
│
├── docs/
│   ├── arhitektura-kompletna.md
│   ├── json-api-specifikacija.md
│   ├── database-objekti.md
│   └── PROJEKTNA-STRUKTURA.md
│
├── ERPAccounting.sln
├── .gitignore
├── LICENSE
├── README.md
└── docker-compose.yml
```

---

## Dependency Flow

```
API → Application → Domain
         ↓
   Infrastructure → Domain
         ↓
      Common (used by all)
```

### Rules
1. **Domain** nema dependency na druge projekte
2. **Application** zavisi od Domain
3. **Infrastructure** zavisi od Domain
4. **API** zavisi od Application i Infrastructure
5. **Common** je shared utility layer

---

## Project Files (.csproj)

### ERPAccounting.API.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../ERPAccounting.Application/ERPAccounting.Application.csproj" />
    <ProjectReference Include="../ERPAccounting.Infrastructure/ERPAccounting.Infrastructure.csproj" />
    <ProjectReference Include="../ERPAccounting.Common/ERPAccounting.Common.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
    <PackageReference Include="Serilog.AspNetCore" Version="7.1.0" />
  </ItemGroup>
</Project>
```

### ERPAccounting.Application.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../ERPAccounting.Domain/ERPAccounting.Domain.csproj" />
    <ProjectReference Include="../ERPAccounting.Common/ERPAccounting.Common.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="AutoMapper" Version="13.0.1" />
    <PackageReference Include="FluentValidation" Version="11.8.0" />
  </ItemGroup>
</Project>
```

### ERPAccounting.Domain.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <!-- NO DEPENDENCIES -->
</Project>
```

### ERPAccounting.Infrastructure.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../ERPAccounting.Domain/ERPAccounting.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0" />
  </ItemGroup>
</Project>
```

### ERPAccounting.Common.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <!-- MINIMAL DEPENDENCIES -->
</Project>
```

---

## Next Steps (FAZA 0.2)

- [ ] Kreiraj .sln fajl
- [ ] Kreiraj sve .csproj fajlove
- [ ] Instaliraj NuGet pakete
- [ ] Setup appsettings.json
- [ ] Konfiguriraj Docker

---

**Status:** ✅ FAZA 0.1 COMPLETED
