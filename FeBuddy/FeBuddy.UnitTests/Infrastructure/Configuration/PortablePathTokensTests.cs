using FeBuddy.Core.Infrastructure.Configuration;

namespace FeBuddy.UnitTests.Infrastructure.Configuration;

/// <summary>
/// Exercises <see cref="PortablePathTokens"/>: folders inside the user's own folders become tokens
/// on export and the importing user's folders on import; everything else passes through.
/// </summary>
public sealed class PortablePathTokensTests
{
	private static readonly PortablePathTokens Alice = new(
	[
		(PortablePathTokens.UserProfileToken, @"C:\Users\alice"),
		(PortablePathTokens.DesktopToken, @"C:\Users\alice\OneDrive\Desktop\"),
		(PortablePathTokens.DocumentsToken, @"C:\Users\alice\Documents"),
	]);

	private static readonly PortablePathTokens Bob = new(
	[
		(PortablePathTokens.DesktopToken, @"D:\Profiles\bob\Desktop"),
		(PortablePathTokens.DocumentsToken, @"D:\Profiles\bob\Documents"),
		(PortablePathTokens.UserProfileToken, @"D:\Profiles\bob"),
	]);

	/// <summary>The most specific folder wins: the Desktop, not the profile it sits in.</summary>
	[Theory]
	[InlineData(@"C:\Users\alice\OneDrive\Desktop\FE-Buddy", @"%DESKTOP%\FE-Buddy")]
	[InlineData(@"c:\users\alice\documents", "%DOCUMENTS%")]
	[InlineData(@"C:\Users\alice\VATSIM\ZOB", @"%USERPROFILE%\VATSIM\ZOB")]
	[InlineData(@"  C:\Users\alice\VATSIM  ", @"%USERPROFILE%\VATSIM")]
	public void tokenize_swaps_the_most_specific_user_folder(string path, string expected) =>
		Assert.Equal(expected, Alice.Tokenize(path));

	/// <summary>A folder outside the user's folders, or one that only shares a prefix, stays as it is.</summary>
	[Theory]
	[InlineData(@"D:\VATSIM\Files")]
	[InlineData(@"C:\Users\alicea\Desktop")]
	public void tokenize_leaves_other_folders_alone(string path) =>
		Assert.Equal(path, Alice.Tokenize(path));

	/// <summary>A blank path is handed back untouched.</summary>
	[Fact]
	public void tokenize_blank_returns_it() =>
		Assert.Equal(" ", Alice.Tokenize(" "));

	/// <summary>A token expands to the importing user's folder, whatever it is called there.</summary>
	[Theory]
	[InlineData(@"%DESKTOP%\FE-Buddy", @"D:\Profiles\bob\Desktop\FE-Buddy")]
	[InlineData("%documents%", @"D:\Profiles\bob\Documents")]
	[InlineData(@"%USERPROFILE%/VATSIM", @"D:\Profiles\bob/VATSIM")]
	public void expand_uses_the_importing_users_folders(string value, string expected) =>
		Assert.Equal(expected, Bob.Expand(value));

	/// <summary>Anything that does not start with a known token passes through.</summary>
	[Theory]
	[InlineData(@"D:\VATSIM\Files")]
	[InlineData(@"%APPDATA%\FE-Buddy")]
	[InlineData(@"%DESKTOPS%\x")]
	[InlineData("")]
	public void expand_leaves_everything_else_alone(string value) =>
		Assert.Equal(value, Bob.Expand(value));

	/// <summary>A token with no folder on this PC is left unexpanded, rather than expanded to nothing.</summary>
	[Fact]
	public void expand_keeps_a_token_with_no_folder()
	{
		PortablePathTokens noDesktop = new([(PortablePathTokens.DesktopToken, "  ")]);

		Assert.Equal(@"%DESKTOP%\x", noDesktop.Expand(@"%DESKTOP%\x"));
	}

	/// <summary>A path under another user's profile lands in the same place in this user's.</summary>
	[Theory]
	[InlineData(@"C:\Users\alice\OneDrive\Desktop\FEB", @"D:\Profiles\bob\Desktop\FEB")]
	[InlineData(@"C:\Users\alice\Desktop", @"D:\Profiles\bob\Desktop")]
	[InlineData(@"c:\users\ALICE\OneDrive\Documents\x", @"D:\Profiles\bob\Documents\x")]
	[InlineData(@"C:\Users\alice\Documents", @"D:\Profiles\bob\Documents")]
	[InlineData(@"C:\Users\alice\VATSIM\DAT", @"D:\Profiles\bob\VATSIM\DAT")]
	[InlineData(@"E:\Users\alice", @"D:\Profiles\bob")]
	[InlineData(@"%DESKTOP%\FEB", @"D:\Profiles\bob\Desktop\FEB")]
	public void localize_moves_another_users_folders_to_this_user(string value, string expected) =>
		Assert.Equal(expected, Bob.Localize(value));

	/// <summary>This user's own profile, shared profiles, and paths outside <c>Users</c> are left alone.</summary>
	[Theory]
	[InlineData(@"C:\Users\bob\Desktop\FEB")]
	[InlineData(@"C:\Users\Public\Documents\FEB")]
	[InlineData(@"C:\Users\Default\x")]
	[InlineData(@"C:\Users")]
	[InlineData(@"C:\Users\")]
	[InlineData(@"Users\alice\x")]
	[InlineData(@"C:\Program Files\x")]
	[InlineData(@"\\server\Users\alice")]
	[InlineData("")]
	public void localize_leaves_other_paths_alone(string value) =>
		Assert.Equal(value, Bob.Localize(value));

	/// <summary>Without a profile folder there is nothing to move a path into.</summary>
	[Fact]
	public void localize_without_a_profile_only_expands()
	{
		PortablePathTokens desktopOnly = new([(PortablePathTokens.DesktopToken, @"D:\Desk")]);

		Assert.Equal(@"C:\Users\alice\x", desktopOnly.Localize(@"C:\Users\alice\x"));
	}

	/// <summary>The current user's tokens round-trip that user's own Desktop.</summary>
	[Fact]
	public void for_current_user_round_trips_the_desktop()
	{
		PortablePathTokens tokens = PortablePathTokens.ForCurrentUser();
		string desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Out");

		Assert.Equal(desktop, tokens.Expand(tokens.Tokenize(desktop)));
	}
}
