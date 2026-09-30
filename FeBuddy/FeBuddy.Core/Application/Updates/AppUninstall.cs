namespace FeBuddy.Core.Application.Updates;

/// <summary>
/// Settings ▸ Uninstall FE-Buddy: the Windows Installer uninstall of this install - the same one
/// Windows runs from Settings ▸ Apps ▸ Installed apps, so it runs the installer's own cleanup
/// (UninstallCleanup.wxs and the credential custom action).
/// </summary>
/// <remarks>
/// <c>msiexec</c> must be started without elevation, the way Windows starts it; Windows Installer
/// asks for administrator permission itself. Started elevated, a standard user who types an
/// administrator's password would run the whole uninstall as that administrator, and the cleanup
/// would find the administrator's FE-Buddy folders and credentials instead of the user's.
/// </remarks>
public static class AppUninstall
{
	/// <summary>
	/// The <c>msiexec</c> arguments that uninstall the product <paramref name="productCode"/> with
	/// Windows Installer's normal UI (it asks the user to confirm).
	/// </summary>
	/// <param name="productCode">The install's ProductCode (<c>InstalledProduct.ProductCode</c>).</param>
	/// <returns>The argument string, or <see langword="null"/> when <paramref name="productCode"/> is not a GUID.</returns>
	public static string? UninstallerArguments(string? productCode) =>
		Guid.TryParse(productCode, out Guid code)
			? $"/x {code.ToString("B").ToUpperInvariant()}"
			: null;
}
