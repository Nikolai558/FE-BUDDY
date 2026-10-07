namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>Where Settings ▸ Import… puts a file's settings.</summary>
public enum ImportDestination
{
	/// <summary>Into a new profile of their own, which FE-Buddy then uses. The default.</summary>
	NewProfile = 0,

	/// <summary>Added to the profile in use, taking the place of what it has where both have a setting.</summary>
	Merge = 1,

	/// <summary>In place of the profile in use's settings: it gets the file's and nothing else.</summary>
	Replace = 2,
}
