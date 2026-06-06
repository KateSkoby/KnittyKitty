using KnittyKitty.Core.Exceptions;
using KnittyKitty.Core.Models;

namespace KnittyKitty.Core.Payments;

public sealed class PaymentStrategyFactory
{
    private readonly IReadOnlyDictionary<PaymentMethod, IPaymentStrategy> _strategies;

    /// Создаёт фабрику платёжных стратегий и регистрирует доступные способы оплаты.
    public PaymentStrategyFactory(IEnumerable<IPaymentStrategy>? strategies = null)
    {
        _strategies = (strategies ?? CreateDefaultStrategies())
            .ToDictionary(strategy => strategy.Method);
    }

    public IPaymentStrategy Get(PaymentMethod method)
    {
        return _strategies.TryGetValue(method, out var strategy)
            ? strategy
            : throw new StoreException($"Payment method '{method}' is not supported.");
    }

    /// Создаёт стандартный набор стратегий оплаты: бонусы, карта и наличные.
    private static IEnumerable<IPaymentStrategy> CreateDefaultStrategies()
    {
        yield return new CashPaymentStrategy();
        yield return new DebitCardPaymentStrategy();
        yield return new BonusPaymentStrategy();
    }
}
