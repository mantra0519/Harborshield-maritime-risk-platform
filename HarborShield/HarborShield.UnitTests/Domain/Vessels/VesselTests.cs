using FluentAssertions;
using HarborShield.Domain.Vessels;

namespace HarborShield.UnitTests.Domain.Vessels;

public class VesselTests
{
    [Fact]
    public void Create_ValidInput_SetsPropertiesAndTimestamp()
    {
        var vessel = Vessel.Create("IMO-9384721", "MV Northern Star", "Panama");

        vessel.ImoNumber.Should().Be("IMO-9384721");
        vessel.Name.Should().Be("MV Northern Star");
        vessel.FlagCountry.Should().Be("Panama");
        vessel.ScreenedAt.Should().BeNull();
        vessel.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("", "Name", "Flag")]
    [InlineData("IMO-1", "", "Flag")]
    [InlineData("IMO-1", "Name", "")]
    public void Create_MissingRequiredField_Throws(string imo, string name, string flag)
    {
        var act = () => Vessel.Create(imo, name, flag);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkScreened_SetsScreenedAtTimestamp()
    {
        var vessel = Vessel.Create("IMO-9384721", "MV Northern Star", "Panama");

        vessel.MarkScreened();

        vessel.ScreenedAt.Should().NotBeNull();
    }
}
