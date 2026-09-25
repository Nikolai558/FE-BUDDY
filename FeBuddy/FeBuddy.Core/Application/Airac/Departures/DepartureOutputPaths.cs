using FeBuddy.Core.Application.Airac.Departures.Models;
using FeBuddy.Core.Domain.Departures.Models;
using FeBuddy.Core.Infrastructure.FileSystem;

namespace FeBuddy.Core.Application.Airac.Departures;

/// <summary>
/// Builds the on-disk output locations for the Departures sub-service, honouring the
/// "Add FE-Buddy_Output folder" preference.
/// </summary>
internal static class DepartureOutputPaths
{
	/// <summary>The sub-service's folder name.</summary>
	private const string RootFolder = "Departure Procedures";

	/// <summary>
	/// The folder one airport's GeoJSON files go in:
	/// <c>…\Departure Procedures\&lt;ARTCC&gt;\&lt;ARPT&gt;</c>.
	/// </summary>
	/// <param name="settings">The parsed settings.</param>
	/// <param name="airportProcedure">The airport + procedure being written.</param>
	/// <returns>The directory.</returns>
	public static string GeojsonDirectory(DepartureSettings settings, DepartureAirportProcedure airportProcedure) =>
		Path.Combine(Root(settings), airportProcedure.Procedure.Artcc, airportProcedure.AirportId);

	/// <summary>The folder the alias file goes in: <c>…\Departure Procedures\Alias</c>.</summary>
	/// <param name="settings">The parsed settings.</param>
	/// <returns>The directory.</returns>
	public static string AliasDirectory(DepartureSettings settings) =>
		Path.Combine(Root(settings), "Alias");

	/// <summary>
	/// A GeoJSON file name, e.g. <c>LAX_DOTSS_Lines.geojson</c>.
	/// </summary>
	/// <param name="airportProcedure">The airport + procedure being written.</param>
	/// <param name="kind"><c>Lines</c>, <c>Symbols</c> or <c>Text</c>.</param>
	/// <returns>The file name.</returns>
	public static string GeojsonFileName(DepartureAirportProcedure airportProcedure, string kind) =>
		$"{airportProcedure.AirportId}_{airportProcedure.Procedure.CodeId}_{kind}.geojson";

	private static string Root(DepartureSettings settings) =>
		ServiceOutputPaths.Resolve(settings.OutputDirectory, settings.AddFeBuddyOutputFolder, RootFolder);
}
