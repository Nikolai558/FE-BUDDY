using System.Collections.Generic;

namespace FeBuddy.Wpf.Infrastructure;

/// <summary>
/// The tab for a sub-service that has been selected but whose backend does not exist yet.
/// </summary>
/// <remarks>
/// It exists so the tab model itself is exercised - opening, closing, ordering, navigation - long
/// before the twenty-odd real sub-services arrive. It is never dirty, never invalid, and never
/// blocks or contributes to a run; the Review tab lists it as carrying no settings.
/// </remarks>
public sealed class PlaceholderSubServiceViewModel : ServiceTabViewModel
{
    private readonly string _title;

    /// <summary>Creates a placeholder tab.</summary>
    /// <param name="title">The sub-service's display name.</param>
    public PlaceholderSubServiceViewModel(string title) => _title = title;

    /// <inheritdoc />
    public override string Title => _title;

    /// <inheritdoc />
    public override bool IsRunnable => false;

    /// <summary>The line shown on the tab in place of a settings menu.</summary>
    public string Message =>
        $"{Title} settings are not built yet. The sub-service is selected, so it will appear here "
        + "with its own options as soon as its backend lands - nothing is produced for it in the "
        + "meantime.";

    /// <inheritdoc />
    public override IReadOnlyList<ServiceReviewSection> BuildReviewSummary() =>
        new[]
        {
            new ServiceReviewSection(Title, Array.Empty<ServiceReviewRow>(), "No settings yet - nothing will be produced for this sub-service."),
        };
}
