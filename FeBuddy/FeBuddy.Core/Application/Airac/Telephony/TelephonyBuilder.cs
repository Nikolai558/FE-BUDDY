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
/// rows a command cannot be made for.
/// </summary>
/// <remarks>
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
	/// Reads every row into an entry, or counts why it was left out.
	/// </summary>
	/// <param name="data">The parsed pages.</param>
	/// <param name="today">The date a U.S. special call sign's expiration is compared with.</param>
	/// <returns>The entries, the counts of rows left out, and any messages.</returns>
	public static TelephonyBuildResult Read(TelephonyDataCollection data, DateOnly today)
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

		return new TelephonyBuildResult(entries, noDesignator, noTelephony, expired, messages);
	}

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
