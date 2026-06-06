namespace KnittyKitty.Core.Models;

public sealed class UserRecord
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public decimal CardBalance { get; set; }

    public decimal CashBalance { get; set; }

    public decimal BonusPoints { get; set; }
}
