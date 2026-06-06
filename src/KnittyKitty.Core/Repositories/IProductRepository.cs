using KnittyKitty.Core.Models;

namespace KnittyKitty.Core.Repositories;

public interface IProductRepository
{
    /// Загружает данные из SQLite-базы и преобразует их в доменные модели.
    Task<IReadOnlyList<ProductBase>> LoadAsync(CancellationToken cancellationToken = default);

    /// Сохраняет актуальное состояние коллекции в SQLite-базу.
    Task SaveAsync(IEnumerable<ProductBase> products, CancellationToken cancellationToken = default);
}
