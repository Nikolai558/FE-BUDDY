using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace FeBuddy.Installer.UI.Setup;

/// <summary>
/// Opens FE-Buddy at the end of setup as the user signed in to Windows, never elevated.
/// </summary>
/// <remarks>
/// <para>
/// The setup window runs in msiexec, with msiexec's account. That is the signed-in user when msiexec
/// is started unelevated (a double-click, FE-Buddy's update and uninstall), but an administrator when
/// it is started elevated - an elevated command prompt, or an update from a FE-Buddy that ran msiexec
/// elevated (3.0.0-beta.12 and earlier). There, a standard user who typed an administrator's
/// password would get FE-Buddy running as that administrator, with the administrator's settings and
/// credentials.
/// </para>
/// <para>
/// So an elevated window asks the desktop (Explorer, which runs as the signed-in user, unelevated)
/// to start it: the desktop folder view's <c>IShellDispatch2.ShellExecute</c>. When that can't be
/// done, FE-Buddy is not opened at all; the user starts it from the Start menu.
/// </para>
/// <para>
/// FeBuddy.Installer.CustomActions links this file in for the stock dialogs' launch
/// (<c>LaunchActions</c>), used on a PC whose policy turns off the setup window.
/// </para>
/// </remarks>
public static class UnelevatedLauncher
{
	private const int CsidlDesktop = 0;
	private const int SwcDesktop = 8;
	private const int SwfoNeedDispatch = 1;
	private const uint SvgioBackground = 0;
	private const int SwShowNormal = 1;

	private static readonly Guid ShellWindowsClsid = new("9BA05972-F6A8-11CF-A442-00A0C90A8F39");
	private static readonly Guid TopLevelBrowserSid = new("4C96BE40-915C-11CF-99D3-00AA004AE837");
	private static readonly Guid DispatchIid = new("00020400-0000-0000-C000-000000000046");

	/// <summary>Starts <paramref name="exePath"/> as the signed-in user, unelevated.</summary>
	/// <param name="exePath">The full path of the program.</param>
	/// <returns><see langword="true"/> when it was started.</returns>
	/// <remarks>Call it on an STA thread (the setup window's).</remarks>
	public static bool Start(string exePath)
	{
		string? folder = Path.GetDirectoryName(exePath);
		try
		{
			if (!IsElevated())
			{
				Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true, WorkingDirectory = folder });
				return true;
			}

			StartFromDesktop(exePath, folder ?? string.Empty);
			return true;
		}
		catch (Exception ex) when (ex is COMException or InvalidCastException or InvalidComObjectException
			or TargetInvocationException or MissingMethodException or System.ComponentModel.Win32Exception
			or InvalidOperationException)
		{
			// Not started: FE-Buddy is installed, and the Start menu opens it.
			return false;
		}
	}

	// An elevated administrator token is in the Administrators role; a UAC-filtered one is not.
	private static bool IsElevated()
	{
		using WindowsIdentity identity = WindowsIdentity.GetCurrent();
		return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
	}

	// Raymond Chen's "How can I launch an unelevated process from my elevated process?": the desktop
	// window's shell view, its folder view's Shell.Application, and that object's ShellExecute, which
	// runs in Explorer.
	private static void StartFromDesktop(string exePath, string folder)
	{
		object? shellWindows = null;
		object? desktop = null;
		IShellBrowser? browser = null;
		IShellView? view = null;
		object? folderView = null;
		object? shell = null;
		try
		{
			shellWindows = Activator.CreateInstance(Type.GetTypeFromCLSID(ShellWindowsClsid, throwOnError: true)!)!;
			object location = CsidlDesktop;
			object root = null!;
			desktop = ((IShellWindows)shellWindows).FindWindowSW(ref location, ref root, SwcDesktop, out _, SwfoNeedDispatch)
				?? throw new InvalidOperationException("No desktop window.");

			Guid service = TopLevelBrowserSid;
			Guid browserIid = typeof(IShellBrowser).GUID;
			browser = (IShellBrowser)((IServiceProvider)desktop).QueryService(ref service, ref browserIid);
			view = browser.QueryActiveShellView();

			Guid dispatchIid = DispatchIid;
			folderView = view.GetItemObject(SvgioBackground, ref dispatchIid);
			shell = folderView.GetType().InvokeMember("Application", BindingFlags.GetProperty, null, folderView, null);
			shell!.GetType().InvokeMember(
				"ShellExecute", BindingFlags.InvokeMethod, null, shell, [exePath, string.Empty, folder, "open", SwShowNormal]);
		}
		finally
		{
			foreach (object? com in new[] { shell, folderView, view, browser, desktop, shellWindows })
			{
				if (com is not null && Marshal.IsComObject(com))
				{
					Marshal.ReleaseComObject(com);
				}
			}
		}
	}

	// IShellWindows' vtable after IDispatch, in order. Only FindWindowSW is typed; the rest keep their slots.
	[ComImport]
	[Guid("85CB6900-4D95-11CF-960C-0080C7F4EE85")]
	[InterfaceType(ComInterfaceType.InterfaceIsDual)]
	private interface IShellWindows
	{
		void Count();

		void Item();

		void NewEnum();

		void Register();

		void RegisterPending();

		void Revoke();

		void OnNavigate();

		void OnActivated();

		[return: MarshalAs(UnmanagedType.IDispatch)]
		object? FindWindowSW(
			[MarshalAs(UnmanagedType.Struct)] ref object location,
			[MarshalAs(UnmanagedType.Struct)] ref object root,
			int windowClass,
			out int windowHandle,
			int options);
	}

	[ComImport]
	[Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IServiceProvider
	{
		[return: MarshalAs(UnmanagedType.Interface)]
		object QueryService(ref Guid service, ref Guid riid);
	}

	// IShellBrowser's vtable (IOleWindow's two methods, then its own), in order. Only
	// QueryActiveShellView is typed; the rest keep their slots.
	[ComImport]
	[Guid("000214E2-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IShellBrowser
	{
		void GetWindow();

		void ContextSensitiveHelp();

		void InsertMenusSB();

		void SetMenuSB();

		void RemoveMenusSB();

		void SetStatusTextSB();

		void EnableModelessSB();

		void TranslateAcceleratorSB();

		void BrowseObject();

		void GetViewStateStream();

		void GetControlWindow();

		void SendControlMsg();

		IShellView QueryActiveShellView();
	}

	// IShellView's vtable (IOleWindow's two methods, then its own), in order. Only GetItemObject is
	// typed; the rest keep their slots.
	[ComImport]
	[Guid("000214E3-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IShellView
	{
		void GetWindow();

		void ContextSensitiveHelp();

		void TranslateAccelerator();

		void EnableModeless();

		void UIActivate();

		void Refresh();

		void CreateViewWindow();

		void DestroyViewWindow();

		void GetCurrentInfo();

		void AddPropertySheetPages();

		void SaveViewState();

		void SelectItem();

		[return: MarshalAs(UnmanagedType.Interface)]
		object GetItemObject(uint item, ref Guid riid);
	}
}
