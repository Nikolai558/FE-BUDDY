using System.Collections.Generic;

namespace FEBuddyLibrary.Generators.NASR;

public static partial class AwyGeojsonGenerator
{
	private enum AirwayOutputBy
	{
		HighLow,
		Type
	}

	private sealed class AirwayGeneratorSettings
	{
		public required string OutputDirectory { get; init; }

		public required AirwayOutputBy OutputBy { get; init; }

		public required bool SplitAtAntimeridian { get; init; }

		public required bool WaypointBuffer { get; init; }
	}

	private static AirwayGeneratorSettings ParseSettings(
		Dictionary<string, string> airwaySettings)
	{
		ArgumentNullException.ThrowIfNull(airwaySettings);

		/*
		 * OutputDirectory
		 */
		if (!airwaySettings.TryGetValue(
				"OutputDirectory",
				out string? outputDirectory) ||
			string.IsNullOrWhiteSpace(outputDirectory))
		{
			throw new ArgumentException(
				"airwaySettings must contain a valid OutputDirectory.",
				nameof(airwaySettings));
		}

		outputDirectory = outputDirectory.Trim();

		/*
		 * OutputBy
		 */
		if (!airwaySettings.TryGetValue(
				"OutputBy",
				out string? outputByValue) ||
			string.IsNullOrWhiteSpace(outputByValue))
		{
			throw new ArgumentException(
				"airwaySettings must contain OutputBy.",
				nameof(airwaySettings));
		}

		AirwayOutputBy outputBy;

		if (outputByValue.Equals(
			"HighLow",
			StringComparison.OrdinalIgnoreCase))
		{
			outputBy = AirwayOutputBy.HighLow;
		}
		else if (outputByValue.Equals(
			"Type",
			StringComparison.OrdinalIgnoreCase))
		{
			outputBy = AirwayOutputBy.Type;
		}
		else
		{
			throw new ArgumentException(
				"OutputBy must be either \"HighLow\" or \"Type\".",
				nameof(airwaySettings));
		}

		/*
		 * SplitAtAntimeridian
		 */
		bool splitAtAntimeridian =
			ParseYesNoSetting(
				airwaySettings,
				"SplitAtAntimeridian");

		/*
		 * WaypointBuffer
		 */
		bool waypointBuffer =
			ParseYesNoSetting(
				airwaySettings,
				"WaypointBuffer");

		return new AirwayGeneratorSettings
		{
			OutputDirectory = outputDirectory,
			OutputBy = outputBy,
			SplitAtAntimeridian = splitAtAntimeridian,
			WaypointBuffer = waypointBuffer
		};
	}

	private static bool ParseYesNoSetting(
		Dictionary<string, string> settings,
		string settingName)
	{
		if (!settings.TryGetValue(
				settingName,
				out string? value) ||
			string.IsNullOrWhiteSpace(value))
		{
			throw new ArgumentException(
				$"airwaySettings must contain {settingName}.",
				nameof(settings));
		}

		value = value.Trim();

		if (value.Equals(
			"Y",
			StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		if (value.Equals(
			"N",
			StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		throw new ArgumentException(
			$"{settingName} must be either \"Y\" or \"N\".",
			nameof(settings));
	}
}