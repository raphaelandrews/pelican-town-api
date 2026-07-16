namespace PelicanTown.SharedKernel.ValueObjects;

public sealed record Phone
{
    public string Value { get; }

    private Phone(string value)
    {
        Value = value;
    }

    public static Phone Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Phone is required.", nameof(value));

        var digits = new string(value.Where(char.IsDigit).ToArray());

        if (digits.Length is < 10 or > 11)
            throw new ArgumentException("Phone must have 10 or 11 digits.", nameof(value));

        return new Phone(digits);
    }
}
