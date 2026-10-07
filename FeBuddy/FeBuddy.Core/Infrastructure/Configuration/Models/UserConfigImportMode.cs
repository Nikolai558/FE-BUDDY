namespace FeBuddy.Core.Infrastructure.Configuration.Models;

/// <summary>What a settings import does with the settings the profile already has (<see cref="UserConfigTransfer.Plan(UserConfigPackage, UserConfigImportMode)"/>).</summary>
public enum UserConfigImportMode
{
	/// <summary>The profile ends up with exactly the file's settings; any the file leaves out go back to their defaults.</summary>
	Replace = 0,

	/// <summary>
	/// The file's settings go over the profile's, and those the file leaves out stay as they are. A
	/// list the file has (custom alias files, say) replaces the profile's whole list.
	/// </summary>
	Merge = 1,
}
