using System;

namespace ERPAccounting.Domain.ValueObjects;

/// <summary>
/// Aggregates tax calculation data for a document or line item.
/// Wraps net amount, tax rate and computed totals using the <see cref="Money"/> value object.
/// </summary>
public sealed class TaxInfo
{
    public Money NetAmount { get; }
    public decimal Rate { get; }

    public TaxInfo(Money netAmount, decimal rate)
    {
        if (rate < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rate), rate, "Tax rate cannot be negative.");
        }

        NetAmount = netAmount;
        Rate = rate;
    }

    public Money TaxAmount => NetAmount.Multiply(Rate);

    public Money GrossAmount => NetAmount.Add(TaxAmount);

    public decimal Percentage => Rate * 100m;

    public static TaxInfo FromPercentage(Money netAmount, decimal percentage)
    {
        if (percentage < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(percentage), percentage, "Tax percentage cannot be negative.");
        }

        return new TaxInfo(netAmount, percentage / 100m);
    }
}
