namespace KnittyKitty.Core.Exceptions;

public class StoreException : Exception
{
    /// Создаёт исключение магазина с поясняющим сообщением.
    public StoreException(string message) : base(message)
    {
    }
}
