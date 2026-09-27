using FeBuddy.Core.Infrastructure.SharedData.Models;

namespace FeBuddy.Core.Infrastructure.Telephony.Models;

/// <summary>What happened when FE-Buddy tried to download fresh copies of the two FAA telephony pages.</summary>
/// <param name="Register">The Section 1 page (the register): required for a Telephony run.</param>
/// <param name="SpecialCallSigns">The Section 4 page (U.S. special call signs): optional.</param>
public sealed record TelephonyRefreshResult(SharedDataRefreshResult Register, SharedDataRefreshResult SpecialCallSigns);
