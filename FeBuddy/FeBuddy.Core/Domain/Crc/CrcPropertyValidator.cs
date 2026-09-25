using FeBuddy.Core.Domain.Crc.Models;

namespace FeBuddy.Core.Domain.Crc;

/// <summary>
/// Validates CRC ERAM GeoJSON property sets against the value ranges and enumerations
/// documented in
/// <see href="https://github.com/KCSanders7070/CRC_GeoJson_Concepts/blob/main/CRC_Geojsons.md">
/// CRC_Geojsons.md</see>.
/// </summary>
/// <remarks>
/// Every <c>Validate*</c> method collects every violation rather than stopping at the first,
/// so the user sees the whole list of problems at once.
/// </remarks>
public static class CrcPropertyValidator
{
	/// <summary>Lowest valid <c>bcg</c> group.</summary>
	public const int MinBcg = 1;

	/// <summary>Highest valid <c>bcg</c> group.</summary>
	public const int MaxBcg = 40;

	/// <summary>Lowest valid <c>filters</c> entry.</summary>
	public const int MinFilter = 0;

	/// <summary>Highest valid <c>filters</c> entry.</summary>
	public const int MaxFilter = 40;

	/// <summary>Lowest valid line <c>thickness</c>.</summary>
	public const int MinThickness = 1;

	/// <summary>Highest valid line <c>thickness</c>.</summary>
	public const int MaxThickness = 3;

	/// <summary>Lowest valid symbol <c>size</c>.</summary>
	public const int MinSymbolSize = 1;

	/// <summary>Highest valid symbol <c>size</c>.</summary>
	public const int MaxSymbolSize = 4;

	/// <summary>Lowest valid text <c>size</c>.</summary>
	public const int MinTextSize = 0;

	/// <summary>Highest valid text <c>size</c>.</summary>
	public const int MaxTextSize = 5;

	/// <summary>
	/// The exact, case-sensitive line style values CRC accepts.
	/// </summary>
	public static readonly IReadOnlyList<string> ValidLineStyles =
	[
		"solid", "shortDashed", "longDashed", "longDashShortDash"
	];

	/// <summary>
	/// The exact, case-sensitive symbol style values CRC accepts.
	/// </summary>
	public static readonly IReadOnlyList<string> ValidSymbolStyles =
	[
		"obstruction1", "obstruction2", "heliport", "nuclear", "emergencyAirport", "radar",
		"iaf", "rnavOnlyWaypoint", "rnav", "airwayIntersections", "ndb", "vor",
		"otherWaypoints", "airport", "satelliteAirport", "tacan"
	];

	/// <summary>Validates a Line Feature's CRC properties.</summary>
	/// <param name="properties">The properties to check.</param>
	/// <returns>Every violation found, or <see cref="CrcPropertyValidationResult.Success"/>.</returns>
	public static CrcPropertyValidationResult ValidateLine(CrcLineProperties properties)
	{
		ArgumentNullException.ThrowIfNull(properties);

		List<string> errors = [];

		ValidateBcg(properties.Bcg, errors);
		ValidateFilters(properties.Filters, errors);

		if (properties.Style is not null &&
			!ValidLineStyles.Contains(properties.Style, StringComparer.Ordinal))
		{
			errors.Add(
				$"Line 'style' value '{properties.Style}' is invalid. " +
				$"Valid values: {string.Join(", ", ValidLineStyles)}.");
		}

		if (properties.Thickness is int thickness &&
			(thickness < MinThickness || thickness > MaxThickness))
		{
			errors.Add(
				$"Line 'thickness' value {thickness} is out of range. " +
				$"Valid range: {MinThickness}-{MaxThickness}.");
		}

		return errors.Count == 0
			? CrcPropertyValidationResult.Success
			: CrcPropertyValidationResult.Failure(errors);
	}

	/// <summary>Validates a Symbol Feature's CRC properties.</summary>
	/// <param name="properties">The properties to check.</param>
	/// <returns>Every violation found, or <see cref="CrcPropertyValidationResult.Success"/>.</returns>
	public static CrcPropertyValidationResult ValidateSymbol(CrcSymbolProperties properties)
	{
		ArgumentNullException.ThrowIfNull(properties);

		List<string> errors = [];

		ValidateBcg(properties.Bcg, errors);
		ValidateFilters(properties.Filters, errors);

		if (properties.Style is not null &&
			!ValidSymbolStyles.Contains(properties.Style, StringComparer.Ordinal))
		{
			errors.Add(
				$"Symbol 'style' value '{properties.Style}' is invalid. " +
				$"Valid values: {string.Join(", ", ValidSymbolStyles)}.");
		}

		if (properties.Size is int size &&
			(size < MinSymbolSize || size > MaxSymbolSize))
		{
			errors.Add(
				$"Symbol 'size' value {size} is out of range. " +
				$"Valid range: {MinSymbolSize}-{MaxSymbolSize}.");
		}

		return errors.Count == 0
			? CrcPropertyValidationResult.Success
			: CrcPropertyValidationResult.Failure(errors);
	}

	/// <summary>Validates a Text Feature's CRC properties.</summary>
	/// <param name="properties">The properties to check.</param>
	/// <returns>Every violation found, or <see cref="CrcPropertyValidationResult.Success"/>.</returns>
	public static CrcPropertyValidationResult ValidateText(CrcTextProperties properties)
	{
		ArgumentNullException.ThrowIfNull(properties);

		List<string> errors = [];

		ValidateBcg(properties.Bcg, errors);
		ValidateFilters(properties.Filters, errors);

		if (properties.Text is null || properties.Text.Count == 0)
		{
			errors.Add(
				"Text 'text' is required and must contain at least one entry; CRC cannot " +
				"auto-assign it, and a Text feature without it will not display.");
		}

		if (properties.Size is int size &&
			(size < MinTextSize || size > MaxTextSize))
		{
			errors.Add(
				$"Text 'size' value {size} is out of range. " +
				$"Valid range: {MinTextSize}-{MaxTextSize}.");
		}

		// xOffset / yOffset accept any integer (CRC spec), so there is nothing to range-check.

		return errors.Count == 0
			? CrcPropertyValidationResult.Success
			: CrcPropertyValidationResult.Failure(errors);
	}

	/// <summary>Validates the values of an <c>isLineDefaults</c> Feature.</summary>
	/// <param name="defaults">The defaults.</param>
	/// <returns>The validation result.</returns>
	public static CrcPropertyValidationResult ValidateLineDefaults(CrcLineDefaults defaults)
	{
		ArgumentNullException.ThrowIfNull(defaults);
		return ValidateLine(defaults.ToFeatureProperties());
	}

	/// <summary>Validates the values of an <c>isSymbolDefaults</c> Feature.</summary>
	/// <param name="defaults">The defaults.</param>
	/// <returns>The validation result.</returns>
	public static CrcPropertyValidationResult ValidateSymbolDefaults(CrcSymbolDefaults defaults)
	{
		ArgumentNullException.ThrowIfNull(defaults);
		return ValidateSymbol(defaults.ToFeatureProperties());
	}

	/// <summary>Validates the values of an <c>isTextDefaults</c> Feature.</summary>
	/// <param name="defaults">The defaults.</param>
	/// <returns>The validation result.</returns>
	/// <remarks>Checked directly rather than through <see cref="ValidateText"/>, which requires the <c>text</c> a defaults Feature never has.</remarks>
	public static CrcPropertyValidationResult ValidateTextDefaults(CrcTextDefaults defaults)
	{
		ArgumentNullException.ThrowIfNull(defaults);

		List<string> errors = [];

		ValidateBcg(defaults.Bcg, errors);
		ValidateFilters(defaults.Filters, errors);

		if (defaults.Size < MinTextSize || defaults.Size > MaxTextSize)
		{
			errors.Add(
				$"Text 'size' value {defaults.Size} is out of range. " +
				$"Valid range: {MinTextSize}-{MaxTextSize}.");
		}

		return errors.Count == 0
			? CrcPropertyValidationResult.Success
			: CrcPropertyValidationResult.Failure(errors);
	}

	/// <summary>
	/// Validates the shared <c>bcg</c> property common to all feature kinds.
	/// </summary>
	private static void ValidateBcg(int? bcg, List<string> errors)
	{
		if (bcg is int value && (value < MinBcg || value > MaxBcg))
		{
			errors.Add($"'bcg' value {value} is out of range. Valid range: {MinBcg}-{MaxBcg}.");
		}
	}

	/// <summary>
	/// Validates the shared <c>filters</c> property common to all feature kinds. Unlike every
	/// other property, CRC can never auto-assign this, so an empty or null list is itself an
	/// error.
	/// </summary>
	private static void ValidateFilters(IReadOnlyList<int>? filters, List<string> errors)
	{
		if (filters is null || filters.Count == 0)
		{
			errors.Add(
				"'filters' is required and must contain at least one entry; CRC cannot " +
				"auto-assign it, and a feature without it will not display.");
			return;
		}

		foreach (int filter in filters)
		{
			if (filter < MinFilter || filter > MaxFilter)
			{
				errors.Add(
					$"'filters' entry {filter} is out of range. " +
					$"Valid range: {MinFilter}-{MaxFilter}.");
			}
		}
	}

	/// <summary>
	/// Throws when <paramref name="result"/> has any errors, listing every one under
	/// <paramref name="heading"/>.
	/// </summary>
	/// <param name="result">The validation result.</param>
	/// <param name="heading">What was being validated, e.g. <c>Invalid CRC defaults under 'Crc.High.Line'</c>.</param>
	/// <param name="paramName">The argument that held the invalid values, when there is one.</param>
	/// <exception cref="ArgumentException">Thrown when the result is not valid.</exception>
	public static void ThrowIfInvalid(CrcPropertyValidationResult result, string heading, string? paramName = null)
	{
		if (!result.IsValid)
		{
			throw new ArgumentException(
				heading + ":" + Environment.NewLine + string.Join(Environment.NewLine, result.Errors),
				paramName);
		}
	}
}
