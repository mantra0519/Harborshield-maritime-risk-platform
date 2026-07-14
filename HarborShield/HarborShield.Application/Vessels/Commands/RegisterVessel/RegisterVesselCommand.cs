using HarborShield.Contracts.Vessels;
using Mediator;

namespace HarborShield.Application.Vessels.Commands.RegisterVessel;

public record RegisterVesselCommand(string ImoNumber, string Name, string FlagCountry) : IRequest<VesselResponse>;
