namespace FeBuddy.Core.Domain.Crc.Models;

/// <summary>
/// The outcome of validating a CRC ERAM property set against
/// <see href="https://github.com/KCSanders7070/CRC_GeoJson_Concepts/blob/main/CRC_Geojsons.md">
/// CRC_Geojsons.md</see>.
/// </summary>
/// <remarks>
/// Carries every violation found, not just the first, so a caller (the GUI's save button,
/// eventually) can report all problems to the user at once instead of one at a time.
/// </remarks>
public sealed record CrcPropertyValidationResult
{
	/// <summary>
	/// A reusable result representing a property set with no violations.
	/// </summary>
	public static readonly CrcPropertyValidationResult Success = new()
	{
		Errors = Array.Empty<string>()
	};

	/// <summary>
	/// Every validation failure found, in the order the properties were checked. Empty when
	/// the property set is valid.
	/// </summary>
	public required IReadOnlyList<string> Errors { get; init; }

	/// <summary>
	/// Gets a value indicating whether the property set passed validation
	/// (i.e. <see cref="Errors"/> is empty).
	/// </summary>
	public bool IsValid => Errors.Count == 0;

	/// <summary>
	/// Creates a failed result from one or more validation error messages.
	/// </summary>
	/// <param name="errors">The validation failure messages.</param>
	/// <returns>A <see cref="CrcPropertyValidationResult"/> with <see cref="IsValid"/> false.</returns>
	public static CrcPropertyValidationResult Failure(IReadOnlyList<string> errors) =>
		new() { Errors = errors };
}
