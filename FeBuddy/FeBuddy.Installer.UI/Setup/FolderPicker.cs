using System;
using System.Runtime.InteropServices;

namespace FeBuddy.Installer.UI.Setup;

/// <summary>
/// Windows' own folder picker (the Explorer-style dialog, <c>IFileOpenDialog</c> with
/// <c>FOS_PICKFOLDERS</c>). .NET Framework's WPF has none, and WinForms' FolderBrowserDialog is the
/// old tree view.
/// </summary>
public static class FolderPicker
{
	private const uint FosPickFolders = 0x20;
	private const uint FosForceFileSystem = 0x40;
	private const uint FosNoChangeDir = 0x8;
	private const uint SigdnFileSysPath = 0x80058000;
	private const int ErrorCancelled = unchecked((int)0x800704C7);

	/// <summary>Asks for a folder.</summary>
	/// <param name="owner">The owning window's handle.</param>
	/// <param name="title">The dialog's title.</param>
	/// <param name="startFolder">The folder to open in, if it exists.</param>
	/// <returns>The chosen folder, or <see langword="null"/> when cancelled.</returns>
	public static string? Pick(IntPtr owner, string title, string? startFolder)
	{
		IFileOpenDialog dialog = (IFileOpenDialog)new FileOpenDialogCoClass();
		try
		{
			dialog.GetOptions(out uint options);
			dialog.SetOptions(options | FosPickFolders | FosForceFileSystem | FosNoChangeDir);
			dialog.SetTitle(title);

			if (!string.IsNullOrWhiteSpace(startFolder) && System.IO.Directory.Exists(startFolder)
				&& SHCreateItemFromParsingName(startFolder!, IntPtr.Zero, typeof(IShellItem).GUID, out IShellItem? start) == 0
				&& start is not null)
			{
				dialog.SetFolder(start);
			}

			int hr = dialog.Show(owner);
			if (hr == ErrorCancelled)
			{
				return null;
			}

			Marshal.ThrowExceptionForHR(hr);
			dialog.GetResult(out IShellItem item);
			item.GetDisplayName(SigdnFileSysPath, out IntPtr path);
			try
			{
				return Marshal.PtrToStringUni(path);
			}
			finally
			{
				Marshal.FreeCoTaskMem(path);
			}
		}
		finally
		{
			Marshal.ReleaseComObject(dialog);
		}
	}

	[DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
	private static extern int SHCreateItemFromParsingName(
		string path, IntPtr bindContext, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IShellItem? item);

	[ComImport]
	[Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
	private class FileOpenDialogCoClass
	{
	}

	// IFileDialog's vtable, in order. Only the members used are typed; the rest keep their slots.
	[ComImport]
	[Guid("D57C7288-D4AD-4768-BE02-9D969532D960")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IFileOpenDialog
	{
		[PreserveSig]
		int Show(IntPtr parent);

		void SetFileTypes(uint count, IntPtr filterSpec);

		void SetFileTypeIndex(uint index);

		void GetFileTypeIndex(out uint index);

		void Advise(IntPtr events, out uint cookie);

		void Unadvise(uint cookie);

		void SetOptions(uint options);

		void GetOptions(out uint options);

		void SetDefaultFolder(IShellItem item);

		void SetFolder(IShellItem item);

		void GetFolder(out IShellItem item);

		void GetCurrentSelection(out IShellItem item);

		void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string name);

		void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string name);

		void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title);

		void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string text);

		void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string label);

		void GetResult(out IShellItem item);
	}

	[ComImport]
	[Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IShellItem
	{
		void BindToHandler(IntPtr bindContext, ref Guid handler, ref Guid riid, out IntPtr result);

		void GetParent(out IShellItem parent);

		void GetDisplayName(uint form, out IntPtr name);

		void GetAttributes(uint mask, out uint attributes);

		void Compare(IShellItem other, uint hint, out int order);
	}
}
