# ERPAccounting.Common

**Shared Utilities & Cross-Cutting Concerns**

## Odgovornosti
- Custom exceptions
- Constants
- Extension methods
- Common models
- Helpers

## Struktura
```
Exceptions/
├── ConflictException.cs        # Za 409 Conflict (ETag)
├── NotFoundException.cs
└── ValidationException.cs

Constants/
├── ApiRoutes.cs
└── ErrorMessages.cs

Extensions/
└── ExceptionHandlingExtensions.cs

Models/
├── ApiResponse.cs
├── PaginationMetadata.cs
└── ErrorResponse.cs
```

## Key Classes

### ConflictException (KRITIČNO)
```csharp
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
```

Koristi se za ETag konkurentnost - vraća HTTP 409.
