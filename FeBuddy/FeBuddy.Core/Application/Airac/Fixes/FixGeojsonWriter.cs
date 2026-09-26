using FeBuddy.Core.Application.Airac.Fixes.Models;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Fixes;
using FeBuddy.Core.Domain.Fixes.Models;
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Domain.Geo.Models;
using FeBuddy.Core.Infrastructure.Geojson;

using NetTopologySuite.Features;

namespace FeBuddy.Core.Application.Airac.Fixes;

/// <summary>
/// Generates the Fixes GeoJSON output: Symbols and Text only, one Point per fix.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="FixOutputBy.All"/> writes at most <c>Fix_Symbols.geojson</c> and
/// <c>Fix_Text.geojson</c>. <see cref="FixOutputBy.FixUse"/>, <see cref="FixOutputBy.Chart"/> and
/// <see cref="FixOutputBy.ChartAndFixUse"/> instead write one Symbols/Text pair per group present
/// (see <see cref="FixOutputFiles"/>). Either way, a file that would hold no fixes is not written
/// at all (see <see cref="GeojsonFileSet"/>).
/// </para>
/// <para>
/// The ROI limits this output only: the caller filters with <see cref="FilterToRoi"/> before
/// calling <see cref="Generate"/>. Each file goes in the GeoJSON folder, or the vNAS one when the
/// user marked it for vNAS. Only a file chosen for CRC-ERAM defaults gets an isDefaults Feature; a
/// Symbol Feature never carries its own <c>style</c> - the file's CRC defaults carry it.
/// </para>
/// </remarks>
public static class FixGeojsonWriter
{
	/// <summary>The known fix use names, indexed for the order <see cref="FixOutputBy.FixUse"/>-mode files are written in.</summary>
	private static readonly IReadOnlyDictionary<string, int> FixUseOrder =
		FixUses.All.Select((name, index) => (name, index)).ToDictionary(pair => pair.name, pair => pair.index, StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Keeps the fixes whose own coordinates fall inside the ROI.
	/// </summary>
	/// <param name="fixes">Every built fix.</param>
	/// <param name="roi">The ROI, or <see langword="null"/> for no filtering.</param>
	/// <returns>The fixes the GeoJSON output covers.</returns>
	public static IReadOnlyList<Fix> FilterToRoi(IReadOnlyList<Fix> fixes, RegionOfInterest? roi) =>
		roi is null
			? fixes
			: [.. fixes.Where(fix => RoiFilter.Contains(roi, fix.Latitude, fix.Longitude))];

	/// <summary>
	/// Generates every GeoJSON file called for by <paramref name="settings"/>.
	/// </summary>
	/// <param name="fixes">The fixes in scope - already ROI-filtered by the caller.</param>
	/// <param name="settings">The parsed Fixes settings.</param>
	/// <returns>The files written and how many rendered Features each holds.</returns>
	public static FixGeojsonGenerateResult Generate(IReadOnlyList<Fix> fixes, FixSettings settings)
	{
		ArgumentNullException.ThrowIfNull(fixes);
		ArgumentNullException.ThrowIfNull(settings);

		GeojsonFileSet files = new(settings.CoordinatePrecision);

		if (fixes.Count == 0)
		{
			return new FixGeojsonGenerateResult(files);
		}

		switch (settings.OutputBy)
		{
			case FixOutputBy.All:
				WriteGroup(fixes, FixOutputFiles.AllClass, FixOutputFiles.Symbols, FixOutputFiles.Text, settings, files);
				break;

			case FixOutputBy.FixUse:
				GenerateByFixUse(fixes, settings, files);
				break;

			case FixOutputBy.Chart:
				GenerateByChart(fixes, settings, files);
				break;

			case FixOutputBy.ChartAndFixUse:
				GenerateByCombination(fixes, settings, files);
				break;
		}

		return new FixGeojsonGenerateResult(files);
	}

	private static void GenerateByFixUse(IReadOnlyList<Fix> fixes, FixSettings settings, GeojsonFileSet files)
	{
		foreach (IGrouping<string, Fix> group in fixes
			.GroupBy(fix => FixUses.Token(fix.FixUse), StringComparer.OrdinalIgnoreCase)
			.Where(group => !settings.ExcludedFixUses.Contains(group.Key))
			.OrderBy(group => FixUseOrder.TryGetValue(group.Key, out int index) ? index : int.MaxValue)
			.ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
		{
			WriteGroup(
				[.. group], group.Key,
				FixOutputFiles.GroupKey(group.Key, CrcFeatureKind.Symbol),
				FixOutputFiles.GroupKey(group.Key, CrcFeatureKind.Text),
				settings, files);
		}
	}

	private static void GenerateByChart(IReadOnlyList<Fix> fixes, FixSettings settings, GeojsonFileSet files)
	{
		// A fix with several charts belongs in every one of its chart groups, so this cannot be a
		// plain GroupBy: it is built by hand, one list per chart token seen.
		Dictionary<string, List<Fix>> byChart = new(StringComparer.OrdinalIgnoreCase);

		foreach (Fix fix in fixes)
		{
			foreach (string chartToken in FixCharts.TokensFor(fix.Charts))
			{
				if (!byChart.TryGetValue(chartToken, out List<Fix>? group))
				{
					group = [];
					byChart[chartToken] = group;
				}

				group.Add(fix);
			}
		}

		foreach (string chartToken in byChart.Keys
			.Where(token => !settings.ExcludedCharts.Contains(token))
			.Order(StringComparer.OrdinalIgnoreCase))
		{
			WriteGroup(
				byChart[chartToken], chartToken,
				FixOutputFiles.GroupKey(chartToken, CrcFeatureKind.Symbol),
				FixOutputFiles.GroupKey(chartToken, CrcFeatureKind.Text),
				settings, files);
		}
	}

	private static void GenerateByCombination(IReadOnlyList<Fix> fixes, FixSettings settings, GeojsonFileSet files)
	{
		foreach (FixCombination combination in settings.Combinations)
		{
			List<Fix> matches = [.. fixes.Where(fix =>
				FixCharts.TokensFor(fix.Charts).Contains(combination.Chart, StringComparer.OrdinalIgnoreCase)
				&& FixUses.Token(fix.FixUse).Equals(combination.FixUse, StringComparison.OrdinalIgnoreCase))];

			WriteGroup(
				matches, combination.Group,
				FixOutputFiles.GroupKey(combination.Group, CrcFeatureKind.Symbol),
				FixOutputFiles.GroupKey(combination.Group, CrcFeatureKind.Text),
				settings, files);
		}
	}

	private static void WriteGroup(
		IReadOnlyList<Fix> fixes,
		string crcClass,
		string symbolsKey,
		string textKey,
		FixSettings settings,
		GeojsonFileSet files)
	{
		if (settings.EmitSymbols)
		{
			GenerateSymbols(fixes, settings, crcClass, symbolsKey, files);
		}

		if (settings.EmitText)
		{
			GenerateText(fixes, settings, crcClass, textKey, files);
		}
	}

	private static void GenerateSymbols(IReadOnlyList<Fix> fixes, FixSettings settings, string crcClass, string fileKey, GeojsonFileSet files)
	{
		FeatureCollection collection = [];

		if (settings.Vnas.HasCrcDefaults(fileKey))
		{
			collection.Add(CrcFeatureFactory.CreateDefaultsFeature(settings.SymbolDefaults[crcClass]));
		}

		foreach (Fix fix in fixes)
		{
			AttributesTable attributes = [];
			AddFebProperties(attributes, fix, settings, forTextFile: false);
			collection.Add(new Feature(Wgs84.Point(fix.Latitude, fix.Longitude), attributes));
		}

		WriteFile(collection, fixes.Count, settings, fileKey, files);
	}

	private static void GenerateText(IReadOnlyList<Fix> fixes, FixSettings settings, string crcClass, string fileKey, GeojsonFileSet files)
	{
		FeatureCollection collection = [];

		if (settings.Vnas.HasCrcDefaults(fileKey))
		{
			collection.Add(CrcFeatureFactory.CreateDefaultsFeature(settings.TextDefaults[crcClass]));
		}

		foreach (Fix fix in fixes)
		{
			AttributesTable attributes = new()
			{
				{ "text", new[] { fix.FixId } }
			};

			AddFebProperties(attributes, fix, settings, forTextFile: true);
			collection.Add(new Feature(Wgs84.Point(fix.Latitude, fix.Longitude), attributes));
		}

		WriteFile(collection, fixes.Count, settings, fileKey, files);
	}

	/// <summary>Writes one file, into the GeoJSON or vNAS folder as the user chose.</summary>
	private static void WriteFile(FeatureCollection collection, int renderedCount, FixSettings settings, string fileKey, GeojsonFileSet files)
	{
		string directory = AiracOutputPaths.FileDirectory(settings.OutputDirectory, isGeojson: true, settings.Vnas.IsUploaded(fileKey));
		files.Write(collection, renderedCount, directory, $"{fileKey}.geojson");
	}

	/// <summary>
	/// Adds the selected <c>feb.*</c> properties to a Feature.
	/// </summary>
	/// <param name="attributes">The Feature's attribute table.</param>
	/// <param name="fix">The fix being written.</param>
	/// <param name="settings">The parsed settings.</param>
	/// <param name="forTextFile">
	/// <see langword="true"/> for the Text file, which skips the identifier - its <c>text</c>
	/// array already carries it.
	/// </param>
	private static void AddFebProperties(AttributesTable attributes, Fix fix, FixSettings settings, bool forTextFile)
	{
		FebProperties.Add(attributes, settings.IncludeFebCustomProperties, settings.FebProperties, property =>
			forTextFile && property == FixFebProperty.FixId ? null : ValueFor(fix, property));
	}

	private static object? ValueFor(Fix fix, FixFebProperty property) => property switch
	{
		FixFebProperty.FixId => fix.FixId,
		FixFebProperty.FixUseCode => fix.FixUse,
		FixFebProperty.Charts => fix.Charts.Count > 0 ? fix.Charts.ToArray() : null,
		_ => null,
	};
}
