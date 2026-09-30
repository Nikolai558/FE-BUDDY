using FeBuddy.Core.Application.Updates;

namespace FeBuddy.UnitTests.Application.Updates;

/// <summary>
/// Covers <see cref="AppUninstall"/>: the msiexec arguments for the install's ProductCode, and none
/// for anything that is not one.
/// </summary>
public sealed class AppUninstallTests
{
	[Theory]
	[InlineData("{15811E26-1D03-4AB3-AE1E-14AEA2A380D7}")]
	[InlineData("{15811e26-1d03-4ab3-ae1e-14aea2a380d7}")]
	[InlineData("15811E26-1D03-4AB3-AE1E-14AEA2A380D7")]
	public void a_product_code_becomes_msiexec_uninstall_arguments(string productCode)
	{
		Assert.Equal("/x {15811E26-1D03-4AB3-AE1E-14AEA2A380D7}", AppUninstall.UninstallerArguments(productCode));
	}

	/// <summary>Nothing recorded, or something that is not a GUID, is never handed to msiexec.</summary>
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("not-a-guid")]
	[InlineData("{15811E26-1D03-4AB3-AE1E-14AEA2A380D7} /qn")]
	public void anything_else_gives_no_arguments(string? productCode)
	{
		Assert.Null(AppUninstall.UninstallerArguments(productCode));
	}
}
