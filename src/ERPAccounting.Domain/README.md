# ERPAccounting.Domain

**Domain Layer** - Core Business Entities

## Odgovornosti
- Domain entities
- Value objects
- Business rules
- Domain specifications

## Struktura
```
Entities/
├── Document.cs
├── DocumentLineItem.cs          # sa RowVersion (ETag)
├── DocumentCost.cs
├── DependentCostLineItem.cs
└── BaseEntity.cs

ValueObjects/
├── Money.cs
└── DocumentStatus.cs

Specifications/
└── DocumentSpecifications.cs
```

## Key Entities

### DocumentLineItem (KRITIČNO)
```csharp
public class DocumentLineItem
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int ArticleId { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    
    [Timestamp]
    public byte[] RowVersion { get; set; }  // Za ETag konkurentnost
    
    public DateTime UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
```
