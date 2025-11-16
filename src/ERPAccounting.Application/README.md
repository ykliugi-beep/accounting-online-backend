# ERPAccounting.Application

**Business Logic Layer**

## Odgovornosti
- Application services
- DTOs (Data Transfer Objects)
- AutoMapper profiles
- Business logic
- Validation

## Struktura
```
Contracts/
├── IDocumentService.cs
├── IDocumentItemService.cs
└── ILookupService.cs

DTOs/
├── DocumentDtos.cs
├── DocumentItemDtos.cs
└── ComboDtos.cs

Services/
├── DocumentService.cs
├── DocumentItemService.cs        # KRITIČNO: ETag handling
├── DocumentCostService.cs
└── CostDistributionService.cs

Mapping/
├── DtoMappingProfile.cs
└── AutoMapperConfiguration.cs
```

## Dependencies
- ERPAccounting.Domain
- ERPAccounting.Common
- AutoMapper
- FluentValidation
