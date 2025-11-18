namespace ERPAccounting.Domain.Enums;

/// <summary>
/// Ways to distribute landed costs and additional expenses.
/// Aligns with combo returned by <c>spNacinDeljenjaTroskovaCombo</c>.
/// </summary>
public enum CostDistributionMethod
{
    ByQuantity = 1,
    ByValue = 2,
    Manual = 3
}
