using HarborShield.Contracts.Vessels;
using Mediator;

namespace HarborShield.Application.Vessels.Queries.ListVessels;

public record ListVesselsQuery : IRequest<IReadOnlyList<VesselResponse>>;
