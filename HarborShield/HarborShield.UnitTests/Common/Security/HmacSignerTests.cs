using FluentAssertions;
using HarborShield.Application.Common.Security;

namespace HarborShield.UnitTests.Common.Security;

public class HmacSignerTests
{
    [Fact]
    public void Verify_MatchingSignature_ReturnsTrue()
    {
        const string secret = "super-secret-webhook-key-123456";
        const string payload = "{\"eventType\":\"RiskCase.Created\"}";

        var signature = HmacSigner.ComputeSignature(secret, payload);

        HmacSigner.Verify(secret, payload, signature).Should().BeTrue();
    }

    [Fact]
    public void Verify_TamperedPayload_ReturnsFalse()
    {
        const string secret = "super-secret-webhook-key-123456";
        const string originalPayload = "{\"riskScore\":10}";
        const string tamperedPayload = "{\"riskScore\":99}";

        var signature = HmacSigner.ComputeSignature(secret, originalPayload);

        HmacSigner.Verify(secret, tamperedPayload, signature).Should().BeFalse();
    }

    [Fact]
    public void Verify_WrongSecret_ReturnsFalse()
    {
        const string payload = "{\"eventType\":\"RiskCase.Created\"}";
        var signature = HmacSigner.ComputeSignature("correct-secret-1234567890", payload);

        HmacSigner.Verify("wrong-secret-1234567890", payload, signature).Should().BeFalse();
    }

    [Fact]
    public void ComputeSignature_SameInputs_IsDeterministic()
    {
        const string secret = "super-secret-webhook-key-123456";
        const string payload = "{\"eventType\":\"RiskCase.Resolved\"}";

        var first = HmacSigner.ComputeSignature(secret, payload);
        var second = HmacSigner.ComputeSignature(secret, payload);

        first.Should().Be(second);
    }
}
