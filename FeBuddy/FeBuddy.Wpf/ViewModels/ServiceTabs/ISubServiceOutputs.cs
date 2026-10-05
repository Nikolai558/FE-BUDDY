using System.ComponentModel;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// Whether a sub-service is in the run and which of its outputs are on: what the General tab's
/// table says for it. A sub-service tab reads its outputs from here rather than owning them, and
/// follows <see cref="INotifyPropertyChanged.PropertyChanged"/> to keep its cards in step.
/// </summary>
public interface ISubServiceOutputs : INotifyPropertyChanged
{
	/// <summary>Whether the sub-service is in the run.</summary>
	bool IsIncluded { get; }

	/// <summary>Whether its alias file is written.</summary>
	bool Alias { get; }

	/// <summary>Whether its GeoJSON files are written.</summary>
	bool Geojson { get; }

	/// <summary>Whether Procedures writes <c>Procedure_Changes.md</c>.</summary>
	bool ProcedureChanges { get; }

	/// <summary>Whether Procedures writes <c>Procedures.json</c>.</summary>
	bool ProceduresJson { get; }
}
