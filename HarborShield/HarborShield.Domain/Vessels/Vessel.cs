namespace HarborShield.Domain.Vessels;

public class Vessel
{
    public Guid Id { get; private set; }
    public string ImoNumber { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string FlagCountry { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ScreenedAt { get; private set; }

    private Vessel()
    {
    }

    public static Vessel Create(string imoNumber, string name, string flagCountry)
    {
        if (string.IsNullOrWhiteSpace(imoNumber))
            throw new ArgumentException("IMO number is required.", nameof(imoNumber));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Vessel name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(flagCountry))
            throw new ArgumentException("Flag country is required.", nameof(flagCountry));

        return new Vessel
        {
            Id = Guid.NewGuid(),
            ImoNumber = imoNumber,
            Name = name,
            FlagCountry = flagCountry,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void MarkScreened() => ScreenedAt = DateTimeOffset.UtcNow;
}
