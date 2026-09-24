using System.Reflection;
using System.Reflection.Emit;

using FeBuddy.Core.Infrastructure.Platform;

namespace FeBuddy.UnitTests.Infrastructure.Platform;

/// <summary>
/// Covers <see cref="AppVersion"/>: the real version comes from the informational version (build
/// metadata dropped), falling back to the numeric version, then <c>dev</c>.
/// </summary>
public sealed class AppVersionTests
{
	[Fact]
	public void Of_NoAssembly_IsDev()
	{
		Assert.Equal("dev", AppVersion.Of(null));
	}

	[Theory]
	[InlineData("3.0.0-alpha.1", "3.0.0-alpha.1")]
	[InlineData("3.0.0-dev+437d7e3f82fe", "3.0.0-dev")]
	[InlineData(" 3.0.0 ", "3.0.0")]
	public void Of_UsesTheInformationalVersion(string informational, string expected)
	{
		Assert.Equal(expected, AppVersion.Of(DynamicAssembly(new Version(9, 9, 9, 9), informational)));
	}

	[Fact]
	public void Of_NoInformationalVersion_UsesTheNumericVersionsFirstThreeParts()
	{
		Assert.Equal("2.8.1", AppVersion.Of(DynamicAssembly(new Version(2, 8, 1, 0), informational: null)));
	}

	[Fact]
	public void Current_ReadsTheEntryAssembly()
	{
		Assert.Equal(AppVersion.Of(Assembly.GetEntryAssembly()), AppVersion.Current);
	}

	private static Assembly DynamicAssembly(Version version, string? informational)
	{
		AssemblyBuilder builder = AssemblyBuilder.DefineDynamicAssembly(
			new AssemblyName("AppVersionTests_" + Guid.NewGuid().ToString("N")) { Version = version },
			AssemblyBuilderAccess.Run);

		if (informational is not null)
		{
			builder.SetCustomAttribute(new CustomAttributeBuilder(
				typeof(AssemblyInformationalVersionAttribute).GetConstructor([typeof(string)])!,
				[informational]));
		}

		return builder;
	}
}
