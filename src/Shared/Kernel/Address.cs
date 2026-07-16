namespace PelicanTown.SharedKernel.ValueObjects;

public sealed record Address
{
    public string Street { get; }
    public string Number { get; }
    public string? Complement { get; }
    public string Neighborhood { get; }
    public string City { get; }
    public string State { get; }
    public string ZipCode { get; }

    private Address(string street, string number, string? complement, string neighborhood, string city, string state, string zipCode)
    {
        Street = street;
        Number = number;
        Complement = complement;
        Neighborhood = neighborhood;
        City = city;
        State = state;
        ZipCode = zipCode;
    }

    public static Address Create(string street, string number, string neighborhood, string city, string state, string zipCode, string? complement = null)
    {
        if (string.IsNullOrWhiteSpace(street)) throw new ArgumentException("Street is required.", nameof(street));
        if (string.IsNullOrWhiteSpace(number)) throw new ArgumentException("Number is required.", nameof(number));
        if (string.IsNullOrWhiteSpace(neighborhood)) throw new ArgumentException("Neighborhood is required.", nameof(neighborhood));
        if (string.IsNullOrWhiteSpace(city)) throw new ArgumentException("City is required.", nameof(city));
        if (string.IsNullOrWhiteSpace(state)) throw new ArgumentException("State is required.", nameof(state));
        if (string.IsNullOrWhiteSpace(zipCode)) throw new ArgumentException("Zip code is required.", nameof(zipCode));

        return new Address(street, number, complement, neighborhood, city, state, zipCode);
    }
}
