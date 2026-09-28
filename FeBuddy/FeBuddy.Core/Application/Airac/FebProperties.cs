using NetTopologySuite.Features;

namespace FeBuddy.Core.Application.Airac;

/// <summary>
/// The optional <c>feb.*</c> properties a sub-service can add to its GeoJSON Features: FE-Buddy's
/// own metadata (an airway's ID, a procedure's amendment number, ...), which CRC ignores and other
/// tools can read.
/// </summary>
/// <remarks>
/// Each sub-service lists what it offers as an enum (<c>AirwayFebProperty</c>,
/// <c>AirportFebProperty</c>, <c>DepartureFebProperty</c>). A property's name is its enum name
/// with a lower-case first letter: <c>AwyId</c> is chosen in settings as <c>awyId</c> and written
/// as <c>feb.awyId</c>.
/// </remarks>
public static class FebProperties
{
	/// <summary>The name a property is chosen and written under, e.g. <c>awyId</c>.</summary>
	/// <typeparam name="TProperty">The sub-service's property enum.</typeparam>
	/// <param name="property">The property.</param>
	/// <returns>The camelCase name.</returns>
	public static string Name<TProperty>(TProperty property) where TProperty : struct, Enum
	{
		string name = property.ToString();
		return char.ToLowerInvariant(name[0]) + name[1..];
	}

	/// <summary>
	/// Adds <c>feb.&lt;name&gt;</c> for each selected property that has a value on this Feature.
	/// </summary>
	/// <typeparam name="TProperty">The sub-service's property enum.</typeparam>
	/// <param name="attributes">The Feature's attributes.</param>
	/// <param name="include">Whether the user asked for <c>feb.*</c> properties at all.</param>
	/// <param name="selected">The properties the user chose, in the order to write them.</param>
	/// <param name="valueFor">
	/// The property's value on this Feature, or <see langword="null"/> when it does not apply to
	/// this kind of Feature (the property is then left out).
	/// </param>
	public static void Add<TProperty>(
		AttributesTable attributes,
		bool include,
		IEnumerable<TProperty> selected,
		Func<TProperty, object?> valueFor)
		where TProperty : struct, Enum
	{
		if (!include)
		{
			return;
		}

		foreach (TProperty property in selected)
		{
			if (valueFor(property) is { } value)
			{
				attributes.Add($"feb.{Name(property)}", value);
			}
		}
	}

	/// <summary>Finds the property a user-typed name refers to, ignoring case.</summary>
	/// <typeparam name="TProperty">The sub-service's property enum.</typeparam>
	/// <param name="name">The name, e.g. <c>awyId</c>.</param>
	/// <param name="property">The property, when found.</param>
	/// <returns><see langword="true"/> when <paramref name="name"/> names a property.</returns>
	public static bool TryParse<TProperty>(string name, out TProperty property) where TProperty : struct, Enum
	{
		foreach (TProperty candidate in Enum.GetValues<TProperty>())
		{
			if (Name(candidate).Equals(name, StringComparison.OrdinalIgnoreCase))
			{
				property = candidate;
				return true;
			}
		}

		property = default;
		return false;
	}

	/// <summary>Every property's name, in enum order, for error messages.</summary>
	/// <typeparam name="TProperty">The sub-service's property enum.</typeparam>
	/// <returns>The names.</returns>
	public static IEnumerable<string> AllNames<TProperty>() where TProperty : struct, Enum =>
		Enum.GetValues<TProperty>().Select(Name);
}
