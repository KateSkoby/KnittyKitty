using KnittyKitty.Core.Exceptions;

namespace KnittyKitty.Core.Models;

public static class ProductFactory
{
    // Factory keeps external storage independent from concrete product classes.
    /// Создаёт доменный товар нужного типа из записи репозитория.
    public static ProductBase Create(ProductRecord record)
    {
        return record.Type.Trim().ToLowerInvariant() switch
        {
            "toy" => new PlushToy(
                record.Id,
                record.Name,
                record.Category,
                record.Description,
                record.UnitPrice,
                record.StockAmount,
                string.IsNullOrWhiteSpace(record.UnitName) ? "шт." : record.UnitName,
                record.ImagePath,
                record.AvailableColors),
            "weighted" => new WeightedPlushMaterial(
                record.Id,
                record.Name,
                record.Category,
                record.Description,
                record.UnitPrice,
                record.StockAmount,
                string.IsNullOrWhiteSpace(record.UnitName) ? "г" : record.UnitName,
                record.ImagePath,
                record.AvailableColors),
            _ => throw new StoreException($"Unknown product type '{record.Type}'.")
        };
    }

    /// Преобразует доменный товар обратно в запись для сохранения.
    public static ProductRecord ToRecord(ProductBase product)
    {
        return new ProductRecord
        {
            Id = product.Id,
            Type = product.RequiresWeighing ? "weighted" : "toy",
            Name = product.Name,
            Category = product.Category,
            Description = product.Description,
            UnitPrice = product.UnitPrice,
            StockAmount = product.StockAmount,
            UnitName = product.UnitName,
            ImagePath = product.ImagePath,
            AvailableColors = product.AvailableColors.ToArray()
        };
    }
}
