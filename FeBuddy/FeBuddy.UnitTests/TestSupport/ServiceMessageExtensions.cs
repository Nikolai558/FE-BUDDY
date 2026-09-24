using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.UnitTests.TestSupport;

/// <summary>Assertion helpers for the levelled messages a step reports.</summary>
internal static class ServiceMessageExtensions
{
	/// <summary>The text of every Warning and Error message, for asserting on what the user would be warned about.</summary>
	/// <param name="messages">The messages.</param>
	/// <returns>The texts, in order.</returns>
	public static IReadOnlyList<string> WarningTexts(this IEnumerable<ServiceMessage> messages) =>
		messages.Where(m => m.Level is LogLevel.Warning or LogLevel.Error).Select(m => m.Text).ToArray();
}
