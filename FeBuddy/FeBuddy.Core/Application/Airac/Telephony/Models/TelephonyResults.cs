using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Telephony.Models;

namespace FeBuddy.Core.Application.Airac.Telephony.Models;

/// <summary>The outcome of parsing the raw Telephony settings dictionary.</summary>
/// <param name="Settings">The typed, validated settings.</param>
/// <param name="Messages">Non-fatal parsing messages (e.g. unrecognized keys that were ignored).</param>
public sealed record TelephonySettingsParseResult(TelephonySettings Settings, IReadOnlyList<ServiceMessage> Messages);

/// <summary>The outcome of reading the parsed FAA telephony pages into entries.</summary>
/// <param name="Entries">Every operator that gets a card: ICAO assignments in register order, then U.S. special call signs in page order.</param>
/// <param name="NoDesignatorCount">
/// Rows left out because they have no three-letter designator (register) or identifier (U.S.
/// special call sign).
/// </param>
/// <param name="NoTelephonyCount">Rows left out because they have no telephony.</param>
/// <param name="ExpiredCount">U.S. special call signs left out because they have expired.</param>
/// <param name="Messages">Messages collected while reading.</param>
public sealed record TelephonyBuildResult(
	IReadOnlyList<TelephonyEntry> Entries,
	int NoDesignatorCount,
	int NoTelephonyCount,
	int ExpiredCount,
	IReadOnlyList<ServiceMessage> Messages);

/// <summary>The outcome of writing <c>Telephony.txt</c>.</summary>
/// <param name="FilePath">The path written, or <see langword="null"/> when there was no entry to write.</param>
/// <param name="CommandCount">How many commands were written.</param>
/// <param name="MergedCommandCount">How many of those show more than one operator.</param>
public sealed record TelephonyAliasGenerateResult(string? FilePath, int CommandCount, int MergedCommandCount);

/// <summary>
/// The result of running the top-level Telephony sub-service (<c>TelephonyService.Run</c>): what was
/// built and written, plus timing and every message collected along the way.
/// </summary>
public sealed record TelephonyServiceResult : ServiceResult
{
	/// <summary>How many ICAO operators (a designator and a telephony) got a card.</summary>
	public required int IcaoAssignmentCount { get; init; }

	/// <summary>How many U.S. special call signs got a card.</summary>
	public required int SpecialCallSignCount { get; init; }

	/// <summary>Rows left out because they have no three-letter designator (register) or identifier (U.S. special call sign).</summary>
	public required int NoDesignatorCount { get; init; }

	/// <summary>Rows left out because they have no telephony.</summary>
	public required int NoTelephonyCount { get; init; }

	/// <summary>U.S. special call signs left out because they have expired.</summary>
	public required int ExpiredCount { get; init; }

	/// <summary>Full path of <c>Telephony.txt</c>, or <see langword="null"/> when it was not written.</summary>
	public string? AliasFilePath { get; init; }

	/// <summary>How many commands <c>Telephony.txt</c> holds; 0 when it was not written.</summary>
	public int AliasCommandCount { get; init; }

	/// <summary>How many of those commands show more than one operator.</summary>
	public int MergedCommandCount { get; init; }
}
