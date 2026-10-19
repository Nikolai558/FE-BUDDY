using System;
using System.IO;
using System.Threading;

using FeBuddy.Installer.UI.Setup;

using WixToolset.Dtf.WindowsInstaller;

namespace FeBuddy.Installer.CustomActions;

/// <summary>
/// The stock dialogs' "Launch FE-BUDDY" (FeBuddy.UI.wxs, the ExitDialog's Finish): opens the
/// installed FE-Buddy as the signed-in user, never elevated, like the setup window's Finish does.
/// </summary>
/// <remarks>
/// Windows Installer shows the stock dialogs instead of the setup window only on a PC with the
/// MsiDisableEmbeddedUI policy. The launch itself is <see cref="UnelevatedLauncher"/>, linked in
/// from FeBuddy.Installer.UI.
/// </remarks>
public static class LaunchActions
{
	/// <summary>Starts <c>[INSTALLFOLDER]FE-BUDDY.exe</c> as the signed-in user; never fails setup.</summary>
	/// <param name="session">The installer session.</param>
	/// <returns>Always <see cref="ActionResult.Success"/>: FE-Buddy is installed, and the Start menu opens it.</returns>
	[CustomAction]
	public static ActionResult LaunchFeBuddy(Session session)
	{
		try
		{
			string exe = Path.Combine(session["INSTALLFOLDER"], "FE-BUDDY.exe");
			if (!File.Exists(exe))
			{
				session.Log($"LaunchFeBuddy: '{exe}' not found, not launching.");
				return ActionResult.Success;
			}

			// The shell's COM objects need an STA thread; the session stays on this one.
			bool started = false;
			Thread thread = new(() =>
			{
				try
				{
					started = UnelevatedLauncher.Start(exe);
				}
				catch (Exception)
				{
					// Not started: a launch must never take the custom action's process down.
				}
			});
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
			thread.Join();

			session.Log(started ? $"LaunchFeBuddy: started '{exe}'." : $"LaunchFeBuddy: could not start '{exe}'.");
		}
		catch (Exception ex)
		{
			session.Log($"LaunchFeBuddy: could not launch, continuing. {ex}");
		}

		return ActionResult.Success;
	}
}
