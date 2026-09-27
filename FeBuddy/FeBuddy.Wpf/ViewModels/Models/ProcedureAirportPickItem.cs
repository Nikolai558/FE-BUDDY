using FeBuddy.Wpf.Mvvm;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One airport + procedure pair listed on the Procedures tab's Airport + Procedure card, e.g.
/// <c>PIT — ILS OR LOC RWY 28C</c>.
/// </summary>
/// <param name="airport">The airport's FAA identifier.</param>
/// <param name="procedureName">The procedure's base chart name.</param>
/// <param name="label">The display label, e.g. <c>PIT — ILS OR LOC RWY 28C</c>.</param>
public sealed class ProcedureAirportPickItem(string airport, string procedureName, string label) : ObservableObject
{
	private string _airport = airport;
	private string _procedureName = procedureName;
	private string _label = label;

	/// <summary>The airport's FAA identifier.</summary>
	public string Airport { get => _airport; set => SetProperty(ref _airport, value); }

	/// <summary>The procedure's base chart name.</summary>
	public string ProcedureName { get => _procedureName; set => SetProperty(ref _procedureName, value); }

	/// <summary>The display label, e.g. <c>PIT — ILS OR LOC RWY 28C</c>.</summary>
	public string Label { get => _label; set => SetProperty(ref _label, value); }
}
