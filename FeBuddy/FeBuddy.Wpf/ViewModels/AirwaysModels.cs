namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One GeoJSON file written by a real Airways run, shown in the results list.
/// Unlike the rest of this namespace, this is not sample data - it reflects an
/// actual <c>AirwayServiceResult</c>.
/// </summary>
public sealed record AirwaysOutputFileRow(string Name, string FullPath, int FeatureCount);

/// <summary>
/// Every warning from a real Airways run that names the same airway (e.g. "Airway
/// 'T312': ..."), grouped so the results panel doesn't show one flat wall of text.
/// Warnings that don't name a specific airway (e.g. an unrecognized setting) are
/// grouped under "General".
/// </summary>
public sealed record AirwaysWarningGroup(string AirwayId, IReadOnlyList<string> Messages)
{
	public int Count => Messages.Count;
}
