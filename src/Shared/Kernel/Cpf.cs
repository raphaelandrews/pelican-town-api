namespace PelicanTown.SharedKernel.ValueObjects;

public sealed record Cpf
{
    public string Value { get; }

    private Cpf(string value)
    {
        Value = value;
    }

    public static Cpf Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("CPF is required.", nameof(value));

        var digits = new string(value.Where(char.IsDigit).ToArray());

        if (digits.Length != 11)
            throw new ArgumentException("CPF must have 11 digits.", nameof(value));

        if (digits.All(d => d == digits[0]))
            throw new ArgumentException("CPF is invalid.", nameof(value));

        return new Cpf(digits);
    }

    public string Formatted =>
        Convert.ToUInt64(Value, System.Globalization.CultureInfo.InvariantCulture)
            .ToString(@"000\.000\.000\-00", System.Globalization.CultureInfo.InvariantCulture);
}
