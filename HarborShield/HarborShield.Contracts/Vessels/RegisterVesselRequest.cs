namespace HarborShield.Contracts.Vessels;

public record RegisterVesselRequest(string ImoNumber, string Name, string FlagCountry);
