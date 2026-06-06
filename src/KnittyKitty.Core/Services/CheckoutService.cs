using KnittyKitty.Core.Exceptions;
using KnittyKitty.Core.Models;
using KnittyKitty.Core.Payments;
using KnittyKitty.Core.Receipts;

namespace KnittyKitty.Core.Services;

public sealed class CheckoutService
{
    private readonly PaymentStrategyFactory _paymentStrategyFactory;
    private readonly CashbackPolicy _cashbackPolicy;

    /// Создаёт экземпляр CheckoutService и подготавливает его начальное состояние.
    public CheckoutService(
        PaymentStrategyFactory? paymentStrategyFactory = null,
        CashbackPolicy? cashbackPolicy = null)
    {
        _paymentStrategyFactory = paymentStrategyFactory ?? new PaymentStrategyFactory();
        _cashbackPolicy = cashbackPolicy ?? new CashbackPolicy();
    }

    /// Оформляет покупку, списывает средства, сохраняет остатки и формирует чек.
    public async Task<Receipt> CheckoutAsync(
        Customer customer,
        ShoppingCart cart,
        IEnumerable<PaymentPart> requestedPayments,
        IReceiptWriter receiptWriter,
        CancellationToken cancellationToken = default)
    {
        if (cart.IsEmpty)
        {
            throw new InvalidCartOperationException("Cart is empty.");
        }

        foreach (var item in cart.Items)
        {
            item.Product.EnsureCanReserve(item.Amount);
        }

        var payments = AllocatePayments(customer, cart.Total, requestedPayments);
        foreach (var payment in payments)
        {
            _paymentStrategyFactory.Get(payment.Method).Pay(customer, payment.Amount);
        }

        foreach (var item in cart.Items)
        {
            item.Product.Withdraw(item.Amount);
        }

        var cashback = _cashbackPolicy.Calculate(payments);
        customer.BonusCard.AddCashback(cashback);

        var receipt = new Receipt(
            Guid.NewGuid(),
            DateTimeOffset.Now,
            customer.Name,
            cart.Items.Select(item => new ReceiptLine(
                item.Product.Name,
                item.Product.Category,
                item.SelectedColor,
                item.Amount,
                item.Product.UnitName,
                item.Product.UnitPrice,
                item.Total)).ToList(),
            payments,
            cart.Total,
            cashback,
            customer.CashBalance,
            customer.DebitCardBalance,
            customer.BonusCard.Points);

        var receiptPath = await receiptWriter.WriteAsync(receipt, cancellationToken);
        cart.Clear();

        return receipt with { FilePath = receiptPath };
    }

    /// Распределяет оплату заказа по запрошенным способам и доступным балансам.
    private IReadOnlyList<PaymentAllocation> AllocatePayments(
        Customer customer,
        decimal total,
        IEnumerable<PaymentPart> requestedPayments)
    {
        var allocations = new List<PaymentAllocation>();
        var remaining = total;

        foreach (var request in requestedPayments.Where(payment => payment.Amount > 0))
        {
            // Requested amounts are treated as limits, so partial payment can continue with the next method.
            if (remaining <= 0)
            {
                break;
            }

            var strategy = _paymentStrategyFactory.Get(request.Method);
            var available = strategy.GetAvailableAmount(customer);
            var amount = Math.Min(Math.Min(request.Amount, available), remaining);

            if (amount <= 0)
            {
                continue;
            }

            allocations.Add(new PaymentAllocation(request.Method, amount));
            remaining -= amount;
        }

        if (remaining > 0)
        {
            throw new InsufficientFundsException(remaining);
        }

        return allocations;
    }
}
