using HarborShield.Contracts.Vessels;
using Mediator;

namespace HarborShield.Application.Vessels.Queries.GetVesselById;

public record GetVesselByIdQuery(Guid VesselId) : IRequest<VesselResponse>;
