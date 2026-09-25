using System.Windows.Input;
using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// One tab inside a first-tier service screen (see <see cref="TabbedServiceViewModel"/>): the
/// General tab, one tab per selected sub-service, the Preview Settings tab, and (once a run
/// has started) the Review tab.
/// </summary>
/// <remarks>
/// The base owns the two things every tab needs and the rail reads: the dirty flag and the
/// validation state, collapsed into <see cref="Status"/>. Validation is continuous rather than
/// save-time, so a tab turns amber the moment the user edits it and red the moment a value goes
/// missing or invalid - which is what makes the rail usable as an at-a-glance checklist when a
/// service has twenty sub-services open.
/// </remarks>
public abstract class ServiceTabViewModel : ObservableObject
{
	private bool _isDirty;
	private string? _validationError;
	private ServiceTabStatus _status = ServiceTabStatus.Ok;

	/// <summary>The tab's label in the rail, e.g. <c>General</c> or <c>Airways</c>.</summary>
	public abstract string Title { get; }

	/// <summary>
	/// <see langword="true"/> for a tab the rail sets apart from the ones above it with a divider -
	/// the run review, which reports on the settings tabs rather than being one of them.
	/// </summary>
	public virtual bool IsSetApart => false;

	/// <summary>
	/// Per-field validation messages, keyed by the field key a view passes to
	/// <c>FieldState.Error</c>. Bind as <c>{Binding FieldErrors[SwLat]}</c>.
	/// </summary>
	public ServiceFieldErrors FieldErrors { get; } = new();

	/// <summary>
	/// <see langword="false"/> for a tab the run cannot act on - a placeholder for a sub-service
	/// whose backend does not exist yet. Such a tab is never counted as blocking a run.
	/// </summary>
	public virtual bool IsRunnable => true;

	/// <summary><see langword="true"/> when the tab has edits the user has not saved.</summary>
	public bool IsDirty
	{
		get => _isDirty;
		protected set
		{
			if (SetProperty(ref _isDirty, value))
			{
				UpdateStatus();
				CommandManager.InvalidateRequerySuggested();
			}
		}
	}

	/// <summary>The first validation failure on this tab, or <see langword="null"/> when valid.</summary>
	public string? ValidationError
	{
		get => _validationError;
		private set
		{
			if (SetProperty(ref _validationError, value))
			{
				OnPropertyChanged(nameof(HasValidationError));
				UpdateStatus();
			}
		}
	}

	/// <summary>Whether <see cref="ValidationError"/> is set.</summary>
	public bool HasValidationError => ValidationError is not null;

	/// <summary>How the rail should present this tab. Derived from dirty + validation state.</summary>
	public ServiceTabStatus Status
	{
		get => _status;
		private set
		{
			if (SetProperty(ref _status, value))
			{
				OnPropertyChanged(nameof(NeedsAttention));
			}
		}
	}

	/// <summary>Whether the rail should draw this tab in a warning colour.</summary>
	public bool NeedsAttention => Status != ServiceTabStatus.Ok;

	/// <summary>
	/// The tab's contribution to the Preview Settings tab: its settings as plain label / value rows.
	/// </summary>
	/// <returns>Zero or more sections, in display order.</returns>
	public abstract IReadOnlyList<ServicePreviewSection> BuildPreviewSummary();

	/// <summary>
	/// Persists this tab. The base does nothing and reports success; a settings tab overrides it
	/// (see <see cref="SubServiceSettingsViewModel"/>).
	/// </summary>
	/// <returns><see langword="true"/> when the tab is saved (or has nothing to save).</returns>
	public virtual bool Save() => true;

	/// <summary>
	/// Re-runs validation and recomputes <see cref="Status"/>. Called automatically by
	/// <see cref="MarkDirty"/>; call it directly after loading values from config.
	/// </summary>
	public void Revalidate()
	{
		ServiceValidation validation = new();
		Validate(validation);

		FieldErrors.Replace(validation.FieldErrors);
		ValidationError = validation.Messages.FirstOrDefault();
		UpdateStatus();
	}

	/// <summary>
	/// Collects this tab's validation failures. Add a plain message for a tab-level problem, or
	/// a field message so the offending box highlights. The base reports nothing.
	/// </summary>
	/// <param name="validation">The collector to add failures to.</param>
	protected virtual void Validate(ServiceValidation validation)
	{
	}

	/// <summary>
	/// Re-evaluates the tab after a setting changed, and re-validates. Call from every bound
	/// setting's setter.
	/// </summary>
	/// <remarks>
	/// The base takes the pessimistic view - anything that reports a change leaves the tab
	/// dirty. A tab that can compare itself against what it last saved overrides this and
	/// answers honestly, so putting a value back the way it was clears the flag again.
	/// </remarks>
	protected virtual void MarkDirty()
	{
		IsDirty = true;
		Revalidate();
	}

	/// <summary>Clears the dirty flag (after a successful save or a reload) and re-validates.</summary>
	protected virtual void ClearDirty()
	{
		IsDirty = false;
		Revalidate();
	}

	private void UpdateStatus() =>
		Status = HasValidationError || FieldErrors.Any
			? ServiceTabStatus.Invalid
			: IsDirty
				? ServiceTabStatus.Unsaved
				: ServiceTabStatus.Ok;
}
