namespace FeBuddy.Wpf.Infrastructure;

/// <summary>
/// The tab for a sub-service that has been selected but whose backend does not exist yet.
/// </summary>
/// <remarks>
/// It exists so the tab model itself is exercised - opening, closing, ordering, navigation - long
/// before the twenty-odd real sub-services arrive. It is never dirty, never invalid, and never
/// blocks or contributes to a run; the Review tab lists it as carrying no settings.
/// </remarks>
/// <param name="title">The sub-service's display name.</param>
public sealed class PlaceholderSubServiceViewModel(string title) : ServiceTabViewModel
{
	private readonly string _title = title;

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
		[
			new ServiceReviewSection(Title, [], "No settings yet - nothing will be produced for this sub-service."),
		];
}
