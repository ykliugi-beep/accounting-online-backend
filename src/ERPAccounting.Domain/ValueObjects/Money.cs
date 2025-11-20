using System;

namespace ERPAccounting.Domain.ValueObjects;

/// <summary>
/// Strongly typed representation of a monetary amount.
/// Keeps precision aligned with SQL Server <c>money</c> columns (19,4).
/// </summary>
public readonly record struct Money
{
    private const int DefaultPrecision = 4;

    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = "RSD")
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency must be provided.", nameof(currency));
        }

        Amount = decimal.Round(amount, DefaultPrecision, MidpointRounding.AwayFromZero);
        Currency = currency.ToUpperInvariant();
    }

    public static Money Zero(string currency = "RSD") => new(0m, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    private void EnsureSameCurrency(Money other)
    {
        if (!Currency.Equals(other.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Money operations require matching currencies.");
        }
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
