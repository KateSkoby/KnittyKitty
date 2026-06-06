using KnittyKitty.Core.Models;

namespace KnittyKitty.Core.Repositories;

public interface IUserRepository
{
    /// Загружает данные из SQLite-базы и преобразует их в доменные модели.
    Task<IReadOnlyList<UserRecord>> LoadAsync(CancellationToken cancellationToken = default);

    /// Находит пользователя в SQLite-базе по email без учёта регистра.
    Task<UserRecord?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// Добавляет нового пользователя в SQLite-базу.
    Task AddAsync(UserRecord user, CancellationToken cancellationToken = default);

    /// Обновляет данные существующего пользователя в SQLite-базе.
    Task UpdateAsync(UserRecord user, CancellationToken cancellationToken = default);

    /// Сохраняет актуальное состояние коллекции в SQLite-базу.
    Task SaveAsync(IEnumerable<UserRecord> users, CancellationToken cancellationToken = default);
}
