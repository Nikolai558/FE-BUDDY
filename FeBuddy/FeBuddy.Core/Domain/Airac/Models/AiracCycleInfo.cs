namespace FeBuddy.Core.Domain.Airac.Models;

/// <summary>
/// One resolved AIRAC cycle: its identifier, the date it becomes effective, and the exact
/// date string the NASR CSV download URL expects.
/// </summary>
/// <param name="AiracCycleId">
/// The four-digit AIRAC cycle identifier, e.g. <c>2610</c> (cycle 10 of 2026).
/// </param>
/// <param name="NasrCsvEffectiveDate">
/// The effective date in the NASR CSV download URL's <c>dd_MMM_yyyy</c> format,
/// e.g. <c>01_Oct_2026</c>.
/// </param>
/// <param name="EffectiveDateUtc">The date this cycle becomes effective, in UTC.</param>
public sealed record AiracCycleInfo(
	string AiracCycleId,
	string NasrCsvEffectiveDate,
	DateOnly EffectiveDateUtc);
