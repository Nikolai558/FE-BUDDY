using FeBuddy.Wpf.Mvvm;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One airport + departure or arrival pair listed on the Procedure Charts tab's Additional
/// Airport-Specific Procedures to Include card, e.g. <c>LAS — BRWNZ</c>.
/// </summary>
/// <param name="airport">The airport's FAA identifier.</param>
/// <param name="procedureName">The departure or arrival's root name, e.g. <c>BRWNZ</c> for <c>BRWNZ FOUR (RNAV)</c>.</param>
/// <param name="label">The display label, e.g. <c>LAS — BRWNZ</c>.</param>
public sealed class ProcedureAirportPickItem(string airport, string procedureName, string label) : ObservableObject
{
	private string _airport = airport;
	private string _procedureName = procedureName;
	private string _label = label;
	private string? _detail;
	private bool _isMissing;

	/// <summary>The airport's FAA identifier.</summary>
	public string Airport { get => _airport; set => SetProperty(ref _airport, value); }

	/// <summary>The departure or arrival's root name, e.g. <c>BRWNZ</c>.</summary>
	public string ProcedureName { get => _procedureName; set => SetProperty(ref _procedureName, value); }

	/// <summary>The display label, e.g. <c>LAS — BRWNZ</c>.</summary>
	public string Label { get => _label; set => SetProperty(ref _label, value); }

	/// <summary>
	/// What the pick brings in from the selected cycle, e.g. <c>Cycle 2611: BRWNZ FIVE (RNAV)</c>,
	/// or that the cycle has none; <see langword="null"/> until the cycle's metafile is known.
	/// </summary>
	public string? Detail { get => _detail; set => SetProperty(ref _detail, value); }

	/// <summary>Whether the selected cycle has no departure or arrival for the pick, so it brings in nothing.</summary>
	public bool IsMissing { get => _isMissing; set => SetProperty(ref _isMissing, value); }
}
