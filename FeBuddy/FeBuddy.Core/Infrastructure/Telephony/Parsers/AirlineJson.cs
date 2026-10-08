using System.Text.Json;

namespace FeBuddy.Core.Infrastructure.Telephony.Parsers;

/// <summary>What the two virtual airline list parsers share.</summary>
internal static class AirlineJson
{
	/// <summary>A property's trimmed text, or <see langword="null"/> when it is missing, not text, or blank.</summary>
	/// <param name="entry">The airline's JSON object.</param>
	/// <param name="property">The property's name.</param>
	/// <returns>The text, or <see langword="null"/>.</returns>
	public static string? Text(JsonElement entry, string property) =>
		entry.TryGetProperty(property, out JsonElement value)
		&& value.ValueKind == JsonValueKind.String
		&& value.GetString()?.Trim() is { Length: > 0 } text
			? text
			: null;
}
