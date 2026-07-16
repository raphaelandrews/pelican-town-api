namespace PelicanTown.SharedKernel.ValueObjects;

public sealed record Email
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email is required.", nameof(value));

        if (!value.Contains('@') || value.Length < 5)
            throw new ArgumentException("Email is invalid.", nameof(value));

        return new Email(value.Trim().ToLowerInvariant());
    }
}
