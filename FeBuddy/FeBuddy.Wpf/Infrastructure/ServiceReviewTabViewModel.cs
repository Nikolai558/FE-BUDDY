using System.Collections.ObjectModel;
using System.Windows.Input;

namespace FeBuddy.Wpf.Infrastructure;

/// <summary>
/// The <b>Preview Settings</b> tab: a plain rundown of everything the other tabs are set to, and
/// the button that runs the service.
/// </summary>
/// <remarks>
/// The summary is rebuilt from the other tabs every time this tab is opened, so it always
/// reflects the live state rather than a snapshot taken when the tab was created. The run button
/// is supplied by the owning service; this tab only reports whether anything blocks it. What
/// happened during the run itself belongs to the Review tab, which appears once a run starts.
/// </remarks>
/// <param name="title">The tab's rail label, e.g. <c>Review</c>.</param>
/// <param name="runLabel">The run button's label, e.g. <c>Run AIRAC Service</c>.</param>
/// <param name="runCommand">The owning service's run command.</param>
/// <param name="tabs">Supplies the tabs to summarise (this tab is skipped).</param>
public sealed class ServiceReviewTabViewModel(
	string title,
	string runLabel,
	ICommand runCommand,
	Func<IEnumerable<ServiceTabViewModel>> tabs) : ServiceTabViewModel
{
	private readonly Func<IEnumerable<ServiceTabViewModel>> _tabs = tabs;
	private readonly string _title = title;
	private string? _blockingIssue;
	private string? _unsavedNotice;

	/// <inheritdoc />
	public override string Title => _title;

	/// <summary>The run button's label.</summary>
	public string RunLabel { get; } = runLabel;

	/// <summary>The owning service's run command.</summary>
	public ICommand RunCommand { get; } = runCommand;

	/// <summary>The rundown, one section per contributing tab.</summary>
	public ObservableCollection<ServiceReviewSection> Sections { get; } = [];

	/// <summary>Set when a tab is invalid, naming the tabs that need fixing first.</summary>
	public string? BlockingIssue
	{
		get => _blockingIssue;
		private set
		{
			if (SetProperty(ref _blockingIssue, value))
			{
				OnPropertyChanged(nameof(HasBlockingIssue));
			}
		}
	}

	/// <summary>Whether something must be fixed before the service can run.</summary>
	public bool HasBlockingIssue => BlockingIssue is not null;

	/// <summary>Set when a tab has unsaved edits, naming them; the run saves them first.</summary>
	public string? UnsavedNotice
	{
		get => _unsavedNotice;
		private set
		{
			if (SetProperty(ref _unsavedNotice, value))
			{
				OnPropertyChanged(nameof(HasUnsavedNotice));
			}
		}
	}

	/// <summary>Whether any tab has unsaved edits.</summary>
	public bool HasUnsavedNotice => UnsavedNotice is not null;

	/// <summary>Rebuilds the rundown and the blocking / unsaved notices from the current tabs.</summary>
	public void Refresh()
	{
		Sections.Clear();

		List<ServiceTabViewModel> others = [.. _tabs().Where(t => !ReferenceEquals(t, this))];

		foreach (ServiceReviewSection section in others.SelectMany(t => t.BuildReviewSummary()))
		{
			Sections.Add(section);
		}

		string[] invalid = [.. others
			.Where(t => t.Status == ServiceTabStatus.Invalid)
			.Select(t => t.Title)];

		string[] dirty = [.. others
			.Where(t => t.IsDirty && t.Status != ServiceTabStatus.Invalid)
			.Select(t => t.Title)];

		BlockingIssue = invalid.Length == 0
			? null
			: $"Fix the highlighted settings on {Join(invalid)} before running.";

		UnsavedNotice = dirty.Length == 0
			? null
			: $"{Join(dirty)} {(dirty.Length == 1 ? "has" : "have")} unsaved changes. They are saved before the run starts.";
	}

	/// <inheritdoc />
	public override IReadOnlyList<ServiceReviewSection> BuildReviewSummary() => [];

	private static string Join(IReadOnlyList<string> names) =>
		names.Count switch
		{
			1 => names[0],
			2 => $"{names[0]} and {names[1]}",
			_ => $"{string.Join(", ", names.Take(names.Count - 1))} and {names[^1]}",
		};
}
