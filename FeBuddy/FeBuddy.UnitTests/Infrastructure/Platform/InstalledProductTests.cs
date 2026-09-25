using FeBuddy.Core.Application.Launch;
using FeBuddy.Core.Infrastructure.Platform;

namespace FeBuddy.UnitTests.Infrastructure.Platform;

/// <summary>
/// Covers <see cref="InstalledProduct"/> against a fake registry (and once against the real
/// one, read-only), and <see cref="AppEnvironment.IsMsiInstalled"/>.
/// </summary>
public sealed class InstalledProductTests : IDisposable
{
	private readonly Func<string, string?> _realReader = InstalledProduct.ReadValue;
	private readonly Dictionary<string, string?> _registry = new(StringComparer.Ordinal);

	public InstalledProductTests() => InstalledProduct.ReadValue = name => _registry.GetValueOrDefault(name);

	public void Dispose() => InstalledProduct.ReadValue = _realReader;

	[Fact]
	public void values_come_from_the_installers_registry_key()
	{
		_registry["InstallLocation"] = @"C:\Program Files\FE-BUDDY\";
		_registry["ProductSemVer"] = "3.0.0-alpha.1";
		_registry["ProductCode"] = "{11111111-2222-3333-4444-555555555555}";

		Assert.Equal(@"C:\Program Files\FE-BUDDY\", InstalledProduct.InstallLocation);
		Assert.Equal("3.0.0-alpha.1", InstalledProduct.ProductSemVer);
		Assert.Equal("{11111111-2222-3333-4444-555555555555}", InstalledProduct.ProductCode);
	}

	[Theory]
	[InlineData(@"C:\Program Files\FE-BUDDY\", @"C:\Program Files\FE-BUDDY\", true)]
	[InlineData(@"C:\Program Files\FE-BUDDY", @"c:\program files\fe-buddy\", true)]
	[InlineData(@"C:\Program Files\FE-BUDDY\", @"C:\Program Files\FE-BUDDY/", true)]
	[InlineData(@"C:\Program Files\FE-BUDDY\", @"D:\src\FeBuddy.Wpf\bin\Debug\net10.0-windows\", false)]
	[InlineData(null, @"C:\Program Files\FE-BUDDY\", false)]
	[InlineData(" ", @"C:\Program Files\FE-BUDDY\", false)]
	[InlineData(@"C:\Program Files\FE-BUDDY\", "", false)]
	[InlineData("C:\\bad\0path", @"C:\Program Files\FE-BUDDY\", false)]
	public void is_msi_installed_matches_the_running_folder_to_the_install_folder(string? installLocation, string baseDirectory, bool expected)
	{
		_registry["InstallLocation"] = installLocation;

		Assert.Equal(expected, InstalledProduct.IsMsiInstalled(baseDirectory));
	}

	[Fact]
	public void real_registry_reads_without_throwing()
	{
		InstalledProduct.ReadValue = _realReader;

		// Whatever this machine has installed, a read is a string or null, never an exception -
		// and a missing value is null.
		_ = InstalledProduct.InstallLocation;
		Assert.Null(InstalledProduct.ReadValue("NoSuchValue_" + Guid.NewGuid().ToString("N")));
	}

	[Fact]
	public void app_environment_is_msi_installed_checks_this_process_folder()
	{
		InstalledProduct.ReadValue = _realReader;

		// The test runner is never the MSI-installed copy.
		Assert.False(AppEnvironment.IsMsiInstalled);
	}
}
