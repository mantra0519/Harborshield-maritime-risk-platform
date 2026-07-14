using FluentAssertions;
using HarborShield.Domain.RiskCases;

namespace HarborShield.UnitTests.Domain.RiskCases;

public class RiskCaseTests
{
    [Fact]
    public void Create_ValidInput_StartsOpenWithGivenValues()
    {
        var riskCase = RiskCase.Create(
            Guid.NewGuid(), RiskCaseType.RouteDeviation, RiskSeverity.High, 80, ["Some reason"]);

        riskCase.Status.Should().Be(RiskCaseStatus.Open);
        riskCase.RiskScore.Should().Be(80);
        riskCase.Reasons.Should().ContainSingle().Which.Should().Be("Some reason");
        riskCase.ResolvedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_RiskScoreOutOfRange_Throws(int invalidScore)
    {
        var act = () => RiskCase.Create(
            Guid.NewGuid(), RiskCaseType.RouteDeviation, RiskSeverity.High, invalidScore, ["reason"]);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Resolve_OpenCase_SetsResolvedStatusAndTimestamp()
    {
        var riskCase = RiskCase.Create(Guid.NewGuid(), RiskCaseType.TrackingGap, RiskSeverity.Medium, 50, ["reason"]);

        riskCase.Resolve("Confirmed benign");

        riskCase.Status.Should().Be(RiskCaseStatus.Resolved);
        riskCase.ResolutionNotes.Should().Be("Confirmed benign");
        riskCase.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public void Resolve_AlreadyResolvedCase_Throws()
    {
        var riskCase = RiskCase.Create(Guid.NewGuid(), RiskCaseType.TrackingGap, RiskSeverity.Medium, 50, ["reason"]);
        riskCase.Resolve("First resolution");

        var act = () => riskCase.Resolve("Second resolution");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Acknowledge_OpenCase_TransitionsToAcknowledged()
    {
        var riskCase = RiskCase.Create(Guid.NewGuid(), RiskCaseType.SanctionsMatch, RiskSeverity.Critical, 95, ["reason"]);

        riskCase.Acknowledge();

        riskCase.Status.Should().Be(RiskCaseStatus.Acknowledged);
    }

    [Fact]
    public void Acknowledge_AlreadyAcknowledgedCase_Throws()
    {
        var riskCase = RiskCase.Create(Guid.NewGuid(), RiskCaseType.SanctionsMatch, RiskSeverity.Critical, 95, ["reason"]);
        riskCase.Acknowledge();

        var act = riskCase.Acknowledge;

        act.Should().Throw<InvalidOperationException>();
    }
}
