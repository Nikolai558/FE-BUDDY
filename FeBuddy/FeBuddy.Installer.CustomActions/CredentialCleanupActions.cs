using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

using WixToolset.Dtf.WindowsInstaller;

namespace FeBuddy.Installer.CustomActions;

/// <summary>
/// Removes FE-Buddy's saved credentials from Windows Credential Manager on a full uninstall, the way
/// UninstallCleanup.wxs removes its data folders. Never on an upgrade (Package.wxs conditions it
/// like the folder cleanup), so credentials survive every update.
/// </summary>
/// <remarks>
/// This project targets .NET Framework and cannot reference FeBuddy.Core, so the few native calls
/// are repeated here. <see cref="TargetPrefix"/> must stay equal to
/// <c>FeBuddy.Core.Infrastructure.Credentials.CredentialStore.TargetPrefix</c>. Like the folder
/// cleanup, it cleans the credentials of the user who runs the uninstall.
/// </remarks>
public static class CredentialCleanupActions
{
	/// <summary>The start of every FE-Buddy entry's name; see <c>CredentialStore.TargetPrefix</c>.</summary>
	public const string TargetPrefix = "FE-Buddy:credential:";

	private const int CredTypeGeneric = 1;
	private const int ErrorNotFound = 1168;

	/// <summary>Deletes every generic credential named <see cref="TargetPrefix"/>…; never fails the uninstall.</summary>
	/// <param name="session">The installer session.</param>
	/// <returns>Always <see cref="ActionResult.Success"/>: a credential left behind must not block an uninstall.</returns>
	[CustomAction]
	public static ActionResult RemoveFeBuddyCredentials(Session session)
	{
		try
		{
			List<string> targets = FindTargets();
			int removed = 0;

			foreach (string target in targets)
			{
				if (CredDelete(target, CredTypeGeneric, 0))
				{
					removed++;
				}
			}

			session.Log($"RemoveFeBuddyCredentials: removed {removed} of {targets.Count} credential(s).");
		}
		catch (Exception ex)
		{
			session.Log($"RemoveFeBuddyCredentials: could not remove credentials, continuing. {ex}");
		}

		return ActionResult.Success;
	}

	private static List<string> FindTargets()
	{
		List<string> targets = [];

		if (!CredEnumerate(TargetPrefix + "*", 0, out int count, out IntPtr list))
		{
			int error = Marshal.GetLastWin32Error();
			return error == ErrorNotFound ? targets : throw new Win32Exception(error);
		}

		try
		{
			for (int i = 0; i < count; i++)
			{
				IntPtr credential = Marshal.ReadIntPtr(list, i * IntPtr.Size);

				// CREDENTIALW: Flags (4), Type (4), then TargetName.
				int type = Marshal.ReadInt32(credential, 4);
				string? target = Marshal.PtrToStringUni(Marshal.ReadIntPtr(credential, 8));

				if (type == CredTypeGeneric && target is not null && target.StartsWith(TargetPrefix, StringComparison.Ordinal))
				{
					targets.Add(target);
				}
			}
		}
		finally
		{
			CredFree(list);
		}

		return targets;
	}

	[DllImport("advapi32.dll", EntryPoint = "CredEnumerateW", CharSet = CharSet.Unicode, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool CredEnumerate(string filter, int flags, out int count, out IntPtr credentials);

	[DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool CredDelete(string target, int type, int flags);

	[DllImport("advapi32.dll")]
	private static extern void CredFree(IntPtr buffer);
}
