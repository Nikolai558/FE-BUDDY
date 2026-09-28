using System;

using FeBuddy.Versioning;

using WixToolset.Dtf.WindowsInstaller;

namespace FeBuddy.Installer.CustomActions;

/// <summary>The MSI's managed custom actions.</summary>
public static class VersionPolicyActions
{
	/// <summary>
	/// Decides whether this install, upgrade or downgrade is allowed under FE-Buddy's version
	/// policy (<see cref="UpdatePolicy"/>) and records the verdict in <c>VERSIONPOLICY_BLOCKED</c>;
	/// a LaunchCondition in Package.wxs acts on it, so the user sees FE-Buddy's own message rather
	/// than a raw custom-action failure. Runs early in both sequences (After="AppSearch"), once
	/// <c>INSTALLEDPRODUCTSEMVER</c> - the <c>ProductSemVer</c> the installed copy recorded, 2.x
	/// or 3.x - has been read from the registry.
	/// </summary>
	/// <param name="session">The installer session.</param>
	/// <returns>Always <see cref="ActionResult.Success"/>; blocking is the LaunchCondition's job.</returns>
	[CustomAction]
	public static ActionResult EnforceVersionPolicy(Session session)
	{
		string installedVersionText = session["INSTALLEDPRODUCTSEMVER"];
		string candidateVersionText = session["PRODUCTSEMVER"];

		session.Log($"EnforceVersionPolicy: installed='{installedVersionText}' candidate='{candidateVersionText}'");

		bool allowed;
		try
		{
			allowed = UpdatePolicy.IsTransitionAllowed(installedVersionText, candidateVersionText);
		}
		catch (Exception ex)
		{
			// Fail open: a version-parsing bug must never brick an install. Logged loudly so it
			// shows in an msiexec /L*v log.
			session.Log($"EnforceVersionPolicy: unexpected error, allowing install. {ex}");
			allowed = true;
		}

		session["VERSIONPOLICY_BLOCKED"] = allowed ? "0" : "1";
		session.Log($"EnforceVersionPolicy: verdict allowed={allowed}");

		return ActionResult.Success;
	}
}
