using FeBuddy.Versioning;
using Xunit;

namespace FeBuddy.Versioning.Tests
{
    public class UpdatePolicyTests
    {

        [Fact]
        public void NothingInstalled_AnyCandidateAllowed()
        {
            Assert.True(UpdatePolicy.IsTransitionAllowed(null, ProductVersion.Parse("2.8.3")));
            Assert.True(UpdatePolicy.IsTransitionAllowed(null, ProductVersion.Parse("0.0.1-alpha.1")));
        }

        [Theory]
        [InlineData("2.8.3", "2.8.4")]
        [InlineData("2.8.3", "2.8.3")]
        [InlineData("2.8.3", "2.8.4-alpha.1")]
        [InlineData("2.8.4-alpha.1", "2.8.4-alpha.2")]
        [InlineData("2.8.4-alpha.1", "2.8.4")]
        public void EqualOrForwardPrecedence_IsAllowed(string installed, string candidate)
        {
            Assert.True(UpdatePolicy.IsTransitionAllowed(
                ProductVersion.Parse(installed),
                ProductVersion.Parse(candidate)));
        }

        [Theory]
        [InlineData("2.8.4", "2.8.3")]
        [InlineData("2.9.0", "2.8.9")]
        [InlineData("2.8.4", "2.8.4-rc.1")]
        public void Downgrade_FromStable_IsBlocked(string installed, string candidate)
        {
            Assert.False(UpdatePolicy.IsTransitionAllowed(
                ProductVersion.Parse(installed),
                ProductVersion.Parse(candidate)));
        }

        [Theory]
        [InlineData("2.8.4-alpha.1", "2.8.3")]
        [InlineData("2.8.4-beta.2", "2.8.4-alpha.1")]
        [InlineData("2.8.4-rc.1", "2.8.3")]
        public void Downgrade_WhenInstalledIsPrerelease_IsAllowed(string installed, string candidate)
        {
            Assert.True(UpdatePolicy.IsTransitionAllowed(
                ProductVersion.Parse(installed),
                ProductVersion.Parse(candidate)));
        }

        [Fact]
        public void StringOverload_DelegatesToVersionRule()
        {
            Assert.True(UpdatePolicy.IsTransitionAllowed("2.8.3", "2.8.4"));
            Assert.False(UpdatePolicy.IsTransitionAllowed("2.8.4", "2.8.3"));
            Assert.True(UpdatePolicy.IsTransitionAllowed("2.8.4-alpha.1", "2.8.3"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void StringOverload_NoInstalledVersion_TreatedAsFreshInstall(string? installed)
        {
            Assert.True(UpdatePolicy.IsTransitionAllowed(installed, "2.8.3"));
        }

        [Fact]
        public void StringOverload_UnparseableInstalled_FailsOpen()
        {
            Assert.True(UpdatePolicy.IsTransitionAllowed("not-a-version", "2.8.3"));
        }

        [Fact]
        public void StringOverload_UnparseableCandidate_FailsOpen()
        {
            Assert.True(UpdatePolicy.IsTransitionAllowed("2.8.3", "also-not-a-version"));
        }
    }
}
