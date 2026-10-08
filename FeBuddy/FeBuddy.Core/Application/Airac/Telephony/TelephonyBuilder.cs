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
/// it, the virtual airline list's.
/// </summary>
/// <remarks>
/// <para>
/// The virtual airlines come last - the user's own in their order, then the list's (see
/// <see cref="VatsimRadarVirtualAirlines"/>) - so a command they share with a real operator shows the
/// real operator's card first (see <c>TelephonyAliasWriter</c>).
/// </para>
/// <para>
/// Only the first entry for each 3LD (or U.S. special identifier) and telephony is written, whatever
/// its company, country or virtual organization (issues #336, #339): a virtual airline with a real
/// operator's 3LD and telephony - <c>AAL AMERICAN</c>, <c>VIR VIRGIN</c> - is left out, and so is one of
/// the list's with the user's own, and a later repeat of either. The run names each, in an Info message.
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

	/// <summary>How many left-out entries a message names before "and N more".</summary>
	private const int NamedInMessage = 20;

	/// <summary>The forms the FAA prints an expiration date in, e.g. <c>24-Feb-2027</c>, <c>4-Nov-2029</c>.</summary>
	private static readonly string[] ExpirationFormats = ["d-MMM-yyyy", "dd-MMM-yyyy"];

	/// <summary>Where an entry came from, in the order they are written.</summary>
	private enum Source
	{
		/// <summary>The FAA's pages: the register and the U.S. special call signs.</summary>
		Faa,

		/// <summary>The user's own virtual airlines.</summary>
		Yours,

		/// <summary>The virtual airline list.</summary>
		List,
	}

	/// <summary>
	/// Reads every row into an entry, or counts why it was left out, then adds an entry for each
	/// virtual airline.
	/// </summary>
	/// <param name="data">The parsed pages.</param>
	/// <param name="today">The date a U.S. special call sign's expiration is compared with.</param>
	/// <param name="virtualAirlines">The user's virtual airlines, already checked by <see cref="TelephonySettingsParser"/>; <see langword="null"/> for none.</param>
	/// <param name="includeVatsimRadar">Whether to add the virtual airline list's virtual airlines (<see cref="TelephonyDataCollection.VatsimRadarAirlines"/>).</param>
	/// <returns>The entries, the counts of rows left out, and any messages.</returns>
	public static TelephonyBuildResult Read(
		TelephonyDataCollection data,
		DateOnly today,
		IReadOnlyList<VirtualAirline>? virtualAirlines = null,
		bool includeVatsimRadar = false)
	{
		ArgumentNullException.ThrowIfNull(data);

		List<TelephonyEntry> faa = [];
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

			faa.Add(new TelephonyEntry(
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

			faa.Add(new TelephonyEntry(
				TelephonyEntryKind.UsSpecialCallSign,
				Upper(row.Identifier),
				Upper(row.Telephony),
				Upper(row.Agency),
				Upper(row.ExpirationDate)));
		}

		List<TelephonyEntry> yours = [.. (virtualAirlines ?? []).Select(VirtualAirlineEntry)];
		List<TelephonyEntry> list = includeVatsimRadar ? ListEntries(data, messages) : [];

		(List<TelephonyEntry> entries, int fromList) = KeepFirstOfEach(faa, yours, list, messages);

		return new TelephonyBuildResult(entries, noDesignator, noTelephony, expired, messages)
		{
			VatsimRadarVirtualAirlineCount = fromList,
		};
	}

	/// <summary>The virtual airline list's virtual airlines that can be written, saying which can't.</summary>
	private static List<TelephonyEntry> ListEntries(TelephonyDataCollection data, List<ServiceMessage> messages)
	{
		VatsimRadarSelection selection = VatsimRadarVirtualAirlines.Select(data.VatsimRadarAirlines);

		if (selection.LeftOut.Count > 0)
		{
			messages.Add(new ServiceMessage(LogLevel.Info, LogSource,
				$"{selection.LeftOut.Count:N0} virtual airline(s) on the {VatsimRadarVirtualAirlines.ListName} were left out: a 3LD that isn't " +
				$"three letters, or a telephony with no letter or digit ({string.Join(", ", selection.LeftOut)})."));
		}

		return [.. selection.VirtualAirlines.Select(VirtualAirlineEntry)];
	}

	/// <summary>
	/// Keeps only the first entry for each 3LD (or identifier) and telephony - the FAA's first, then the
	/// user's own virtual airlines, then the list's - and says, in a message for each kind, which were left out.
	/// </summary>
	/// <returns>The entries to write, in order, and how many of them came from the list.</returns>
	private static (List<TelephonyEntry> Entries, int FromList) KeepFirstOfEach(
		List<TelephonyEntry> faa,
		List<TelephonyEntry> yours,
		List<TelephonyEntry> list,
		List<ServiceMessage> messages)
	{
		// The values are already trimmed and upper case, as the card prints them.
		Dictionary<(string Identifier, string Telephony), Source> firstFrom = [];
		Dictionary<(Source LeftOut, Source Kept), List<string>> leftOut = [];
		List<TelephonyEntry> kept = [];
		int fromList = 0;

		foreach ((Source source, List<TelephonyEntry> entries) in new[] { (Source.Faa, faa), (Source.Yours, yours), (Source.List, list) })
		{
			foreach (TelephonyEntry entry in entries)
			{
				if (firstFrom.TryGetValue((entry.Identifier, entry.Telephony), out Source first))
				{
					if (!leftOut.TryGetValue((source, first), out List<string>? names))
					{
						leftOut[(source, first)] = names = [];
					}

					names.Add($"{entry.Identifier} {entry.Telephony} ({entry.Organization})");
					continue;
				}

				firstFrom[(entry.Identifier, entry.Telephony)] = source;
				kept.Add(entry);
				fromList += source == Source.List ? 1 : 0;
			}
		}

		foreach (((Source left, Source first), List<string> names) in leftOut.OrderBy(pair => pair.Key.LeftOut).ThenBy(pair => pair.Key.Kept))
		{
			messages.Add(new ServiceMessage(LogLevel.Info, LogSource, LeftOutMessage(left, first, names)));
		}

		return (kept, fromList);
	}

	/// <summary>What the run says about the entries left out for having the 3LD and telephony of one written before.</summary>
	private static string LeftOutMessage(Source left, Source first, List<string> names)
	{
		int count = names.Count;
		string have = count == 1 ? "has" : "have";
		string list = VatsimRadarVirtualAirlines.ListName;

		string text = (left, first) switch
		{
			(Source.Faa, _) =>
				$"{count:N0} FAA telephony row(s) repeat an earlier row's 3LD and telephony, so only the first was written",
			(Source.Yours, Source.Faa) =>
				$"{count:N0} of your virtual airlines {have} the same 3LD and telephony as a real operator, so only the real operator was written",
			(Source.Yours, _) =>
				$"{count:N0} of your virtual airlines {have} the same 3LD and telephony as another of yours, so only the first was written",
			(Source.List, Source.Faa) =>
				$"{count:N0} virtual airline(s) on the {list} {have} the same 3LD and telephony as a real operator, so only the real operator was written",
			(Source.List, Source.Yours) =>
				$"{count:N0} virtual airline(s) on the {list} {have} the same 3LD and telephony as one of yours, so only yours was written",
			_ =>
				$"{count:N0} virtual airline(s) on the {list} repeat an earlier one's 3LD and telephony, so only the first was written",
		};

		string named = string.Join(", ", names.Take(NamedInMessage));
		string more = count > NamedInMessage ? $", and {count - NamedInMessage:N0} more" : string.Empty;

		return $"{text}: {named}{more}.";
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
