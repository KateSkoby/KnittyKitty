namespace KnittyKitty.Core.Models;

public sealed class ProductRecord
{
    public string Id { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public decimal StockAmount { get; set; }

    public string UnitName { get; set; } = string.Empty;

    public string ImagePath { get; set; } = string.Empty;

    public string[] AvailableColors { get; set; } = Array.Empty<string>();
}
