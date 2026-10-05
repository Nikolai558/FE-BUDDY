using System.Collections;
using System.Globalization;
using System.Text;

using NetTopologySuite.Features;

namespace FeBuddy.Core.Infrastructure.Geojson;

/// <summary>
/// A key for a Feature's properties: two attribute tables get the same key exactly when they
/// would write the same GeoJSON properties - the same names with the same values, in any order.
/// It is how Features that carry the same things are found, to join their lines or group their
/// symbols.
/// </summary>
/// <remarks>
/// Arrays compare element by element (a <c>filters</c> list, a <c>text</c> label's lines,
/// <c>feb.charts</c>), numbers by value whatever their type (<c>1</c> and <c>1.0</c> write the
/// same), and a string never matches a number or a <see langword="true"/> that reads the same.
/// </remarks>
public static class AttributesSignature
{
	/// <summary>The key for <paramref name="attributes"/>.</summary>
	/// <param name="attributes">The properties, or <see langword="null"/> for none.</param>
	/// <returns>The key; the same for every table with no properties.</returns>
	public static string Of(IAttributesTable? attributes)
	{
		if (attributes is null || attributes.Count == 0)
		{
			return string.Empty;
		}

		StringBuilder key = new();

		foreach (string name in attributes.GetNames().Order(StringComparer.Ordinal))
		{
			// Each part says how long it is, so no name or value can run into the next.
			key.Append(name.Length).Append(':').Append(name).Append('=');
			AppendValue(key, attributes[name]);
			key.Append(';');
		}

		return key.ToString();
	}

	private static void AppendValue(StringBuilder key, object? value)
	{
		switch (value)
		{
			case null:
				key.Append('-');
				break;

			case string text:
				key.Append('s').Append(text.Length).Append(':').Append(text);
				break;

			case bool flag:
				key.Append(flag ? 't' : 'f');
				break;

			case sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal:
				key.Append('n').Append(Convert.ToDouble(value, CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture));
				break;

			case IEnumerable items:
				key.Append('[');
				foreach (object? item in items)
				{
					AppendValue(key, item);
					key.Append(',');
				}

				key.Append(']');
				break;

			default:
				string other = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
				key.Append('o').Append(other.Length).Append(':').Append(other);
				break;
		}
	}
}
