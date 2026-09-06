using FEBuddyLibrary.Models.Geojson;

namespace FEBuddyLibrary.Services.General;

/// <summary>
/// Validates CRC ERAM GeoJSON property sets against the value ranges and enumerations
/// documented in
/// <see href="https://github.com/KCSanders7070/CRC_GeoJson_Concepts/blob/main/CRC_Geojsons.md">
/// CRC_Geojsons.md</see>.
/// </summary>
/// <remarks>
/// Every <c>Validate*</c> method collects every violation found rather than stopping at the
/// first one, so a caller can report the full list of problems at once (e.g. the GUI's save
/// button will eventually surface all of them together).
/// </remarks>
public static class CrcGeojsonPropertyValidator
{
	private const int MinBcg = 1;
	private const int MaxBcg = 40;
	private const int MinFilter = 0;
	private const int MaxFilter = 40;
	private const int MinThickness = 1;
	private const int MaxThickness = 3;
	private const int MinSymbolSize = 1;
	private const int MaxSymbolSize = 4;
	private const int MinTextSize = 0;
	private const int MaxTextSize = 5;

	/// <summary>
	/// The exact, case-sensitive line style values CRC accepts.
	/// </summary>
	public static readonly IReadOnlyList<string> ValidLineStyles = new[]
	{
		"solid", "shortDashed", "longDashed", "longDashShortDash"
	};

	/// <summary>
	/// The exact, case-sensitive symbol style values CRC accepts.
	/// </summary>
	public static readonly IReadOnlyList<string> ValidSymbolStyles = new[]
	{
		"obstruction1", "obstruction2", "heliport", "nuclear", "emergencyAirport", "radar",
		"iaf", "rnavOnlyWaypoint", "rnav", "airwayIntersections", "ndb", "vor",
		"otherWaypoints", "airport", "satelliteAirport", "tacan"
	};

	/// <summary>
	/// Validates a set of properties for the given <see cref="CrcFeatureKind"/>.
	/// </summary>
	/// <param name="kind">Which feature family <paramref name="properties"/> belongs to.</param>
	/// <param name="properties">
	/// A <see cref="CrcLineProperties"/>, <see cref="CrcSymbolProperties"/>, or
	/// <see cref="CrcTextProperties"/> instance matching <paramref name="kind"/>.
	/// </param>
	/// <returns>The validation result.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when <paramref name="properties"/> is not the type expected for
	/// <paramref name="kind"/>.
	/// </exception>
	public static CrcPropertyValidationResult Validate(CrcFeatureKind kind, object properties)
	{
		return kind switch
		{
			CrcFeatureKind.Line => ValidateLine(AsType<CrcLineProperties>(properties, kind)),
			CrcFeatureKind.Symbol => ValidateSymbol(AsType<CrcSymbolProperties>(properties, kind)),
			CrcFeatureKind.Text => ValidateText(AsType<CrcTextProperties>(properties, kind)),
			_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown CRC feature kind.")
		};
	}

	/// <summary>
	/// Validates a Line feature's CRC properties.
	/// </summary>
	public static CrcPropertyValidationResult ValidateLine(CrcLineProperties properties)
	{
		ArgumentNullException.ThrowIfNull(properties);

		List<string> errors = new();

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

	/// <summary>
	/// Validates a Symbol feature's CRC properties.
	/// </summary>
	public static CrcPropertyValidationResult ValidateSymbol(CrcSymbolProperties properties)
	{
		ArgumentNullException.ThrowIfNull(properties);

		List<string> errors = new();

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

	/// <summary>
	/// Validates a Text feature's CRC properties.
	/// </summary>
	public static CrcPropertyValidationResult ValidateText(CrcTextProperties properties)
	{
		ArgumentNullException.ThrowIfNull(properties);

		List<string> errors = new();

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

		if (properties.XOffset is int xOffset && xOffset < 0)
		{
			errors.Add($"Text 'xOffset' value {xOffset} is invalid; it must be >= 0.");
		}

		if (properties.YOffset is int yOffset && yOffset < 0)
		{
			errors.Add($"Text 'yOffset' value {yOffset} is invalid; it must be >= 0.");
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
	/// Casts <paramref name="properties"/> to <typeparamref name="T"/>, throwing a clear
	/// <see cref="ArgumentException"/> naming the mismatch when the runtime type does not
	/// match the declared <see cref="CrcFeatureKind"/>.
	/// </summary>
	private static T AsType<T>(object properties, CrcFeatureKind kind) where T : class
	{
		ArgumentNullException.ThrowIfNull(properties);

		if (properties is not T typed)
		{
			throw new ArgumentException(
				$"CRC feature kind '{kind}' requires a '{typeof(T).Name}' instance, " +
				$"but a '{properties.GetType().Name}' was supplied.",
				nameof(properties));
		}

		return typed;
	}
}
