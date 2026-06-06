namespace KnittyKitty.Core.Exceptions;

public sealed class InvalidCartOperationException : StoreException
{
    /// Создаёт исключение магазина с поясняющим сообщением.
    public InvalidCartOperationException(string message) : base(message)
    {
    }
}
