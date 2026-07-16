namespace PelicanTown.SharedKernel.ValueObjects;

public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency = "BRL")
    {
        if (amount < 0)
            throw new ArgumentException("Amount must be non-negative.", nameof(amount));
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));

        return new Money(amount, currency.ToUpperInvariant());
    }

    public static Money Zero(string currency = "BRL")
    {
        ArgumentNullException.ThrowIfNull(currency);
        return new Money(0, currency.ToUpperInvariant());
    }

    public static Money operator +(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        if (left.Currency != right.Currency)
            throw new InvalidOperationException("Cannot add money with different currencies.");
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        if (left.Currency != right.Currency)
            throw new InvalidOperationException("Cannot subtract money with different currencies.");
        return new Money(left.Amount - right.Amount, left.Currency);
    }
}
