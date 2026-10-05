namespace FeBuddy.Core.Application.Airac.Telephony;

/// <summary>
/// The one file the Telephony sub-service writes: its alias file, which goes where every alias
/// file goes (<see cref="AiracOutputPaths.AliasDirectory"/>).
/// </summary>
public static class TelephonyOutputFiles
{
	/// <summary>The alias file (<c>TelephonyAliasWriter</c>) - also its file key on the File Names tab.</summary>
	public const string Alias = "Telephony.txt";
}
