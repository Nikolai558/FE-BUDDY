using System.Text;

using FeBuddy.Core.Application.Airac.Departures.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Departures;
using FeBuddy.Core.Domain.Departures.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Core.Application.Airac.Departures;

/// <summary>
/// Generates the <c>Departures.txt</c> alias file: one <c>.FF</c> command per airport +
/// procedure that draws every point on it, e.g.
/// <c>.laxDOTSSf .FF DLREY ENNEY NAANC HAYNK PEVEE HOLTZ DOTSS DOCKR …</c>.
/// </summary>
/// <remarks>
/// <para>
/// The command name is a period, the airport in lower case, the procedure's
/// <see cref="DepartureProcedure.CodeId"/> in upper case, then a lower-case <c>f</c>. Both parts
/// are letters and digits only, which is what CRC's alias parser accepts (<c>\.\w+</c>).
/// </para>
/// <para>
/// Each point appears once. The order carries no meaning to <c>.FF</c>; it is bodies first,
/// then transitions, as met, only so the file reads sensibly and diffs cleanly cycle to cycle.
/// A single-point procedure still gets a command.
/// </para>
/// <para>
/// Unlike Airports, the file follows every filter the user set, ROI included.
/// </para>
/// </remarks>
public static class DepartureAliasWriter
{
	private const string LogSource = "DepartureAliasWriter";

	/// <summary>
	/// Writes the alias file for the airport + procedure pairs in scope.
	/// </summary>
	/// <param name="airportProcedures">The pairs in scope - already filtered by the caller.</param>
	/// <param name="settings">The parsed settings.</param>
	/// <returns>The path written (or <see langword="null"/> when there was nothing to write), the command count, and any messages.</returns>
	public static DepartureAliasGenerateResult Generate(
		IReadOnlyList<DepartureAirportProcedure> airportProcedures,
		DepartureSettings settings)
	{
		ArgumentNullException.ThrowIfNull(airportProcedures);
		ArgumentNullException.ThrowIfNull(settings);

		List<ServiceMessage> messages = [];
		StringBuilder builder = new();
		HashSet<string> writtenCommands = new(StringComparer.OrdinalIgnoreCase);
		int commandCount = 0;

		foreach (DepartureAirportProcedure airportProcedure in airportProcedures)
		{
			if (airportProcedure.Points.Count == 0)
			{
				continue;
			}

			string command = CommandName(airportProcedure);

			if (!writtenCommands.Add(command))
			{
				messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
					$"{DepartureBuilder.Label(airportProcedure.Procedure)} at {airportProcedure.AirportId}: alias command '{command}' was already written and was skipped."));
				continue;
			}

			builder.Append(BuildCommand(airportProcedure)).AppendLine();
			commandCount++;
		}

		if (commandCount == 0)
		{
			return new DepartureAliasGenerateResult(null, 0, messages);
		}

		// The output folder itself, or Upload_to_vNAS when the user marked the file for vNAS.
		string directory = AiracOutputPaths.FileDirectory(
			settings.OutputDirectory, isGeojson: false, settings.Vnas.IsUploaded(DepartureOutputFiles.Alias));
		Directory.CreateDirectory(directory);

		string path = Path.Combine(directory, DepartureOutputFiles.Alias);
		File.WriteAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

		return new DepartureAliasGenerateResult(path, commandCount, messages);
	}

	/// <summary>The alias command name, e.g. <c>.laxDOTSSf</c>.</summary>
	/// <param name="airportProcedure">The airport + procedure.</param>
	/// <returns>The command name, period included.</returns>
	internal static string CommandName(DepartureAirportProcedure airportProcedure) =>
		$".{DepartureNaming.Clean(airportProcedure.AirportId).ToLowerInvariant()}{airportProcedure.Procedure.CodeId}f";

	/// <summary>The whole alias line, e.g. <c>.laxDOTSSf .FF DLREY ENNEY …</c>.</summary>
	/// <param name="airportProcedure">The airport + procedure.</param>
	/// <returns>The line, without a line ending.</returns>
	internal static string BuildCommand(DepartureAirportProcedure airportProcedure) =>
		$"{CommandName(airportProcedure)} .FF {string.Join(' ', airportProcedure.Points.Select(point => point.Id))}";
}
