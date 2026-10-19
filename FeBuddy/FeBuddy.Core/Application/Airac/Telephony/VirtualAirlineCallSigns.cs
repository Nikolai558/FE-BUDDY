using System.Runtime.CompilerServices;

using FeBuddy.Core.Application.Airac.Telephony.Models;

namespace FeBuddy.Core.Application.Airac.Telephony;

/// <summary>
/// A virtual airline list looked up by 3LD and telephony, trimmed and ignoring case - for the
/// Telephony tab, which asks on every keystroke and command re-query whether what its editor holds is
/// on the list. Built once per list (<see cref="For"/>), so each question is a lookup, not a pass over
/// tens of thousands of airlines.
/// </summary>
public sealed class VirtualAirlineCallSigns
{
	private static readonly ConditionalWeakTable<IReadOnlyList<VirtualAirline>, VirtualAirlineCallSigns> Built = new();

	private readonly Dictionary<string, List<VirtualAirline>> _byCallSign;

	private VirtualAirlineCallSigns(Dictionary<string, List<VirtualAirline>> byCallSign) => _byCallSign = byCallSign;

	/// <summary>The lookup of no list: every question misses.</summary>
	public static VirtualAirlineCallSigns Empty { get; } = new(new Dictionary<string, List<VirtualAirline>>(StringComparer.OrdinalIgnoreCase));

	/// <summary>A list's lookup: built the first time it is asked for, then kept with the list.</summary>
	/// <param name="list">The virtual airlines.</param>
	/// <returns>The lookup.</returns>
	public static VirtualAirlineCallSigns For(IReadOnlyList<VirtualAirline> list)
	{
		ArgumentNullException.ThrowIfNull(list);
		return Built.GetValue(list, Build);
	}

	/// <summary>Builds a list's lookup.</summary>
	/// <param name="list">The virtual airlines.</param>
	/// <returns>The lookup.</returns>
	public static VirtualAirlineCallSigns Build(IReadOnlyList<VirtualAirline> list)
	{
		ArgumentNullException.ThrowIfNull(list);

		Dictionary<string, List<VirtualAirline>> byCallSign = new(StringComparer.OrdinalIgnoreCase);

		foreach (VirtualAirline virtualAirline in list)
		{
			string key = Key(virtualAirline.Designator, virtualAirline.Telephony);

			if (!byCallSign.TryGetValue(key, out List<VirtualAirline>? sharing))
			{
				sharing = [];
				byCallSign[key] = sharing;
			}

			sharing.Add(virtualAirline);
		}

		return new VirtualAirlineCallSigns(byCallSign);
	}

	/// <summary>Every virtual airline on the list with this 3LD and telephony, in the list's order.</summary>
	/// <param name="designator">The 3LD.</param>
	/// <param name="telephony">The telephony.</param>
	/// <returns>The virtual airlines; empty when none has them.</returns>
	public IReadOnlyList<VirtualAirline> WithCallSign(string designator, string telephony)
	{
		ArgumentNullException.ThrowIfNull(designator);
		ArgumentNullException.ThrowIfNull(telephony);

		return _byCallSign.TryGetValue(Key(designator, telephony), out List<VirtualAirline>? sharing) ? sharing : [];
	}

	/// <summary>
	/// The first virtual airline with this 3LD and telephony, whatever its virtual organization - as
	/// <see cref="VatsimRadarVirtualAirlines.FindSameCallSign"/> finds it.
	/// </summary>
	/// <param name="designator">The 3LD.</param>
	/// <param name="telephony">The telephony.</param>
	/// <returns>The virtual airline, or <see langword="null"/>.</returns>
	public VirtualAirline? FindSameCallSign(string designator, string telephony) =>
		WithCallSign(designator, telephony) is [VirtualAirline first, ..] ? first : null;

	/// <summary>The virtual airline that is exactly the one given, as <see cref="VatsimRadarVirtualAirlines.FindSame"/> finds it.</summary>
	/// <param name="designator">The 3LD.</param>
	/// <param name="telephony">The telephony.</param>
	/// <param name="organization">The virtual organization.</param>
	/// <returns>The virtual airline, or <see langword="null"/>.</returns>
	public VirtualAirline? FindSame(string designator, string telephony, string organization)
	{
		ArgumentNullException.ThrowIfNull(organization);

		return WithCallSign(designator, telephony).FirstOrDefault(virtualAirline => virtualAirline.IsSameAs(designator, telephony, organization));
	}

	private static string Key(string designator, string telephony) => $"{designator.Trim()}\n{telephony.Trim()}";
}
