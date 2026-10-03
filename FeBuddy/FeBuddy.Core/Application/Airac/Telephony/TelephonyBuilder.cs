using System.Globalization;

using FeBuddy.Core.Application.Airac.Telephony.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Telephony;
using FeBuddy.Core.Domain.Telephony.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Telephony.Models;

namespace FeBuddy.Core.Application.Airac.Telephony;

/// <summary>
/// Reads the parsed FAA telephony pages into <see cref="TelephonyEntry"/> cards, leaving out the
/// rows a command cannot be made for, then adds the user's virtual airlines and, when they include
/// it, the VATSIM-Radar Virtual Airline List's.
/// </summary>
/// <remarks>
/// <para>
/// The virtual airlines come last - the user's own in their order, then the VATSIM-Radar list's
/// (see <see cref="VatsimRadarVirtualAirlines"/>) - so a command they share with a real operator
/// shows the real operator's card first (see <c>TelephonyAliasWriter</c>). A list entry that is the
/// same as one of the user's own is written once, as theirs; one that differs in its 3LD, telephony
/// or virtual organization gets a card of its own.
/// </para>
/// <para>Left out, and counted for the run's summary:</para>
/// <list type="bullet">
///   <item>a register row with no three-letter designator (the FAA prints <c>...</c> or <c>--</c>) - its telephony alone is not looked up;</item>
///   <item>a row with no telephony - a designator with nothing to say;</item>
///   <item>a U.S. special call sign whose expiration date is before today.</item>
/// </list>
/// <para>
/// A U.S. special call sign with no expiration (<c>N/A</c>) is kept. One whose date cannot be read
/// is kept too, with a warning naming it, rather than silently dropped.
/// </para>
/// </remarks>
public static class TelephonyBuilder
{
	private const string LogSource = "TelephonyBuilder";

	/// <summary>The forms the FAA prints an expiration date in, e.g. <c>24-Feb-2027</c>, <c>4-Nov-2029</c>.</summary>
	private static readonly string[] ExpirationFormats = ["d-MMM-yyyy", "dd-MMM-yyyy"];

	/// <summary>
	/// Reads every row into an entry, or counts why it was left out, then adds an entry for each
	/// virtual airline.
	/// </summary>
	/// <param name="data">The parsed pages.</param>
	/// <param name="today">The date a U.S. special call sign's expiration is compared with.</param>
	/// <param name="virtualAirlines">The user's virtual airlines, already checked by <see cref="TelephonySettingsParser"/>; <see langword="null"/> for none.</param>
	/// <param name="includeVatsimRadar">Whether to add the VATSIM-Radar Virtual Airline List's virtual airlines (<see cref="TelephonyDataCollection.VatsimRadarAirlines"/>).</param>
	/// <returns>The entries, the counts of rows left out, and any messages.</returns>
	public static TelephonyBuildResult Read(
		TelephonyDataCollection data,
		DateOnly today,
		IReadOnlyList<VirtualAirline>? virtualAirlines = null,
		bool includeVatsimRadar = false)
	{
		ArgumentNullException.ThrowIfNull(data);

		List<TelephonyEntry> entries = [];
		List<ServiceMessage> messages = [];
		int noDesignator = 0;
		int noTelephony = 0;
		int expired = 0;

		foreach (TelephonyHtmlDataModel.Assignment row in data.Assignments)
		{
			if (!TelephonyNaming.IsThreeLetterDesignator(row.ThreeLetterDesignator))
			{
				noDesignator++;
				continue;
			}

			if (string.IsNullOrWhiteSpace(row.Telephony))
			{
				noTelephony++;
				continue;
			}

			entries.Add(new TelephonyEntry(
				TelephonyEntryKind.IcaoAssignment,
				Upper(row.ThreeLetterDesignator),
				Upper(row.Telephony),
				Upper(row.Company),
				Upper(row.Country)));
		}

		foreach (TelephonyHtmlDataModel.SpecialCallSign row in data.SpecialCallSigns)
		{
			if (TelephonyNaming.CommandName(row.Identifier) is null)
			{
				noDesignator++;
				continue;
			}

			if (string.IsNullOrWhiteSpace(row.Telephony))
			{
				noTelephony++;
				continue;
			}

			if (IsExpired(row, today, messages))
			{
				expired++;
				continue;
			}

			entries.Add(new TelephonyEntry(
				TelephonyEntryKind.UsSpecialCallSign,
				Upper(row.Identifier),
				Upper(row.Telephony),
				Upper(row.Agency),
				Upper(row.ExpirationDate)));
		}

		IReadOnlyList<VirtualAirline> own = virtualAirlines ?? [];

		foreach (VirtualAirline virtualAirline in own)
		{
			entries.Add(VirtualAirlineEntry(virtualAirline));
		}

		int vatsimRadarCount = includeVatsimRadar ? AddVatsimRadar(data, own, entries, messages) : 0;

		return new TelephonyBuildResult(entries, noDesignator, noTelephony, expired, messages)
		{
			VatsimRadarVirtualAirlineCount = vatsimRadarCount,
		};
	}

	/// <summary>
	/// Adds the VATSIM-Radar list's virtual airlines that can be written and are not already the
	/// user's own, saying which were left out and which were the user's already.
	/// </summary>
	/// <returns>How many were added.</returns>
	private static int AddVatsimRadar(TelephonyDataCollection data, IReadOnlyList<VirtualAirline> own, List<TelephonyEntry> entries, List<ServiceMessage> messages)
	{
		VatsimRadarSelection selection = VatsimRadarVirtualAirlines.Select(data.VatsimRadarAirlines);
		List<string> alreadyYours = [];
		int added = 0;

		foreach (VirtualAirline virtualAirline in selection.VirtualAirlines)
		{
			if (VatsimRadarVirtualAirlines.FindSame(own, virtualAirline.Designator, virtualAirline.Telephony, virtualAirline.Organization) is not null)
			{
				alreadyYours.Add($"{Upper(virtualAirline.Designator)} ({Upper(virtualAirline.Telephony)}, {Upper(virtualAirline.Organization)})");
				continue;
			}

			entries.Add(VirtualAirlineEntry(virtualAirline));
			added++;
		}

		if (selection.LeftOut.Count > 0)
		{
			messages.Add(new ServiceMessage(LogLevel.Info, LogSource,
				$"{selection.LeftOut.Count:N0} virtual airline(s) on the {VatsimRadarVirtualAirlines.ListName} were left out: a 3LD that isn't " +
				$"three letters, or a telephony with no letter or digit ({string.Join(", ", selection.LeftOut)})."));
		}

		if (alreadyYours.Count > 0)
		{
			string verb = alreadyYours.Count == 1 ? "is" : "are";

			messages.Add(new ServiceMessage(LogLevel.Info, LogSource,
				$"{alreadyYours.Count:N0} of your virtual airlines {verb} also on the {VatsimRadarVirtualAirlines.ListName}, so each was written " +
				$"once, as yours: {string.Join("; ", alreadyYours)}."));
		}

		return added;
	}

	/// <summary>A virtual airline's card, every value upper case as the card prints it.</summary>
	private static TelephonyEntry VirtualAirlineEntry(VirtualAirline virtualAirline) => new(
		TelephonyEntryKind.VirtualAirline,
		Upper(virtualAirline.Designator),
		Upper(virtualAirline.Telephony),
		Upper(virtualAirline.Organization),
		string.Empty);

	/// <summary>
	/// Whether a U.S. special call sign expired before <paramref name="today"/>. No expiration
	/// (<c>N/A</c> or blank) never expires; a date that cannot be read is kept, with a warning.
	/// </summary>
	private static bool IsExpired(TelephonyHtmlDataModel.SpecialCallSign row, DateOnly today, List<ServiceMessage> messages)
	{
		string text = row.ExpirationDate.Trim();

		if (text.Length == 0 || text.Equals("N/A", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		if (DateOnly.TryParseExact(text, ExpirationFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly expiration))
		{
			return expiration < today;
		}

		messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
			$"U.S. special call sign '{row.Telephony}' ({row.Identifier}) has an expiration date FE-Buddy can't read, '{text}', " +
			"so it was kept."));
		return false;
	}

	private static string Upper(string text) => text.Trim().ToUpperInvariant();
}
