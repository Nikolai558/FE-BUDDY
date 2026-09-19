namespace FeBuddy.Core.Models.Services.Airac;

/// <summary>
/// A progress notification from <c>AiracService.RunAsync</c> as it dispatches to each
/// selected sub-service.
/// </summary>
/// <param name="SubService">The sub-service the message is about, e.g. <c>"Airways"</c>.</param>
/// <param name="Message">A short human-readable status line for the run panel and the log.</param>
/// <param name="PercentComplete">Overall completion 0-100 when known, otherwise <see langword="null"/>.</param>
public sealed record AiracServiceProgress(string SubService, string Message, double? PercentComplete = null);
