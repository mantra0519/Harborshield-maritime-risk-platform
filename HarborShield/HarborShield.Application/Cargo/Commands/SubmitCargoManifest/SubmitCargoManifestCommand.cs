using HarborShield.Contracts.Cargo;
using Mediator;

namespace HarborShield.Application.Cargo.Commands.SubmitCargoManifest;

public record SubmitCargoManifestCommand(
    Guid VesselId,
    string OriginPort,
    string DestinationPort,
    string ShipperName,
    string ReceiverName,
    double DeclaredWeightKg,
    bool IsHazardous) : IRequest<CargoManifestResponse>;
