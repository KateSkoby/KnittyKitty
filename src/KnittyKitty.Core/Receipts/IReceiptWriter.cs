using KnittyKitty.Core.Models;

namespace KnittyKitty.Core.Receipts;

public interface IReceiptWriter
{
    /// Сохраняет чек в целевое хранилище и возвращает путь или идентификатор результата.
    Task<string> WriteAsync(Receipt receipt, CancellationToken cancellationToken = default);
}
