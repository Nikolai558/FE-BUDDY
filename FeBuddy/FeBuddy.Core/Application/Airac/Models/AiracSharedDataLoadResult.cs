using FeBuddy.Core.Application.Models;

namespace FeBuddy.Core.Application.Airac.Models;

/// <summary>What <c>AiracSharedDataLoader</c> got for a run: the parsed data, and what to tell the user about it.</summary>
/// <typeparam name="T">The parsed data's type.</typeparam>
/// <param name="Data">The parsed data, or <see langword="null"/> when there is no usable copy.</param>
/// <param name="Messages">
/// What the run's Review tab says about the download: a note when it is fresh, an advisory warning
/// when an older copy was used, an error when there is none.
/// </param>
public sealed record AiracSharedDataLoadResult<T>(T? Data, IReadOnlyList<ServiceMessage> Messages)
	where T : class;
