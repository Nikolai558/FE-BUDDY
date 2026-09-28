using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Core.Application.Airac.Models;

/// <summary>
/// The result of reading the <c>FileNames</c> block: the new names, plus a message for every entry
/// that was left out.
/// </summary>
/// <param name="FileNames">The new names.</param>
/// <param name="Messages">A <see cref="LogLevel.Warning"/> for each entry that names no file FE-Buddy can rename.</param>
public sealed record OutputFileNamesParseResult(
	OutputFileNames FileNames,
	IReadOnlyList<ServiceMessage> Messages);
