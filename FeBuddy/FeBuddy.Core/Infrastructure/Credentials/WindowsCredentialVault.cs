using System.ComponentModel;
using System.Runtime.InteropServices;

using FeBuddy.Core.Infrastructure.Credentials.Models;

using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace FeBuddy.Core.Infrastructure.Credentials;

/// <summary>
/// Windows Credential Manager, the store Windows itself, Git for Windows and the GitHub CLI keep
/// sign-ins in. Each entry is a generic credential, encrypted by Windows with the signed-in user's
/// key (DPAPI): readable by that user on this PC only, never by another account or another PC even
/// with a copy of the files. The user can see and delete the entries in Control Panel ▸ Credential
/// Manager ▸ Windows Credentials.
/// </summary>
/// <remarks>
/// Entries are saved with local-machine persistence, so they never roam to other PCs with a
/// roaming profile. A generic credential holds at most <see cref="MaxSecretBytes"/> bytes.
/// </remarks>
public sealed class WindowsCredentialVault : ICredentialVault
{
	/// <summary>The most a generic credential can hold (<c>CRED_MAX_CREDENTIAL_BLOB_SIZE</c>).</summary>
	public const int MaxSecretBytes = 5 * 512;

	private const int CredTypeGeneric = 1;
	private const int CredPersistLocalMachine = 2;
	private const int ErrorNotFound = 1168;

	/// <inheritdoc />
	public void Write(string target, string userName, byte[] secret)
	{
		ArgumentException.ThrowIfNullOrEmpty(target);
		ArgumentNullException.ThrowIfNull(userName);
		ArgumentNullException.ThrowIfNull(secret);

		if (secret.Length > MaxSecretBytes)
		{
			throw new ArgumentException($"Windows Credential Manager holds at most {MaxSecretBytes} bytes per entry.", nameof(secret));
		}

		// Allocated inside the try, so running out of memory part-way still frees what was allocated.
		IntPtr targetPtr = IntPtr.Zero;
		IntPtr userPtr = IntPtr.Zero;
		IntPtr blobPtr = IntPtr.Zero;

		try
		{
			targetPtr = Marshal.StringToCoTaskMemUni(target);
			userPtr = Marshal.StringToCoTaskMemUni(userName);
			blobPtr = Marshal.AllocCoTaskMem(Math.Max(secret.Length, 1));
			Marshal.Copy(secret, 0, blobPtr, secret.Length);

			NativeCredential credential = new()
			{
				Type = CredTypeGeneric,
				TargetName = targetPtr,
				CredentialBlobSize = secret.Length,
				CredentialBlob = blobPtr,
				Persist = CredPersistLocalMachine,
				UserName = userPtr,
			};

			if (!CredWrite(ref credential, 0))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
		}
		finally
		{
			if (blobPtr != IntPtr.Zero)
			{
				// Do not leave a copy of the secret in freed memory.
				Marshal.Copy(new byte[secret.Length], 0, blobPtr, secret.Length);
				Marshal.FreeCoTaskMem(blobPtr);
			}

			// Freeing IntPtr.Zero does nothing.
			Marshal.FreeCoTaskMem(userPtr);
			Marshal.FreeCoTaskMem(targetPtr);
		}
	}

	/// <inheritdoc />
	public byte[]? Read(string target)
	{
		ArgumentException.ThrowIfNullOrEmpty(target);

		if (!CredRead(target, CredTypeGeneric, 0, out IntPtr credentialPtr))
		{
			int error = Marshal.GetLastWin32Error();
			return error == ErrorNotFound ? null : throw new Win32Exception(error);
		}

		try
		{
			return CopySecret(Marshal.PtrToStructure<NativeCredential>(credentialPtr));
		}
		finally
		{
			CredFree(credentialPtr);
		}
	}

	/// <inheritdoc />
	public bool Delete(string target)
	{
		ArgumentException.ThrowIfNullOrEmpty(target);

		if (CredDelete(target, CredTypeGeneric, 0))
		{
			return true;
		}

		int error = Marshal.GetLastWin32Error();
		return error == ErrorNotFound ? false : throw new Win32Exception(error);
	}

	/// <inheritdoc />
	public IReadOnlyList<VaultEntry> Enumerate(string prefix)
	{
		ArgumentException.ThrowIfNullOrEmpty(prefix);

		if (!CredEnumerate(prefix + "*", 0, out int count, out IntPtr listPtr))
		{
			int error = Marshal.GetLastWin32Error();
			return error == ErrorNotFound ? [] : throw new Win32Exception(error);
		}

		try
		{
			List<VaultEntry> entries = new(count);

			for (int i = 0; i < count; i++)
			{
				NativeCredential credential = Marshal.PtrToStructure<NativeCredential>(Marshal.ReadIntPtr(listPtr, i * IntPtr.Size));
				string target = Marshal.PtrToStringUni(credential.TargetName) ?? string.Empty;

				// The filter's wildcard is not case-sensitive; only FE-Buddy's exact prefix counts.
				if (credential.Type == CredTypeGeneric && target.StartsWith(prefix, StringComparison.Ordinal))
				{
					entries.Add(new VaultEntry(target, CopySecret(credential)));
				}
			}

			return entries;
		}
		finally
		{
			CredFree(listPtr);
		}
	}

	private static byte[] CopySecret(NativeCredential credential)
	{
		byte[] secret = new byte[credential.CredentialBlobSize];

		if (secret.Length > 0)
		{
			Marshal.Copy(credential.CredentialBlob, secret, 0, secret.Length);
		}

		return secret;
	}

	[DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool CredWrite(ref NativeCredential credential, int flags);

	[DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool CredRead(string target, int type, int flags, out IntPtr credential);

	[DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool CredDelete(string target, int type, int flags);

	[DllImport("advapi32.dll", EntryPoint = "CredEnumerateW", CharSet = CharSet.Unicode, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool CredEnumerate(string filter, int flags, out int count, out IntPtr credentials);

	[DllImport("advapi32.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	private static extern void CredFree(IntPtr buffer);

	/// <summary>The native <c>CREDENTIALW</c> structure.</summary>
	[StructLayout(LayoutKind.Sequential)]
	private struct NativeCredential
	{
		public int Flags;
		public int Type;
		public IntPtr TargetName;
		public IntPtr Comment;
		public FILETIME LastWritten;
		public int CredentialBlobSize;
		public IntPtr CredentialBlob;
		public int Persist;
		public int AttributeCount;
		public IntPtr Attributes;
		public IntPtr TargetAlias;
		public IntPtr UserName;
	}
}
