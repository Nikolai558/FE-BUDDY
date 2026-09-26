using FeBuddy.Wpf.Mvvm;

using FeBuddy.Core.Application.Airac.Fixes;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One chart + fix use combination listed on the Fixes tab's Combinations card, shown in the
/// <c>ChartAndFixUse</c> file layout.
/// </summary>
/// <param name="chart">The chart's token, e.g. <c>ENROUTE-LOW</c> or <c>NO-CHART</c>.</param>
/// <param name="fixUse">The fix use's token, e.g. <c>WYPNT</c>.</param>
/// <param name="label">The display label, e.g. <c>ENROUTE LOW + WYPNT</c>.</param>
/// <param name="fileNames">The files this combination writes, for display, e.g. <c>Fix_ENROUTE-LOW-WYPNT_Symbols / _Text</c>.</param>
public sealed class FixCombinationItem(string chart, string fixUse, string label, string fileNames) : ObservableObject
{
	private string _chart = chart;
	private string _fixUse = fixUse;
	private string _label = label;
	private string _fileNames = fileNames;

	/// <summary>The chart's token, e.g. <c>ENROUTE-LOW</c> or <c>NO-CHART</c>.</summary>
	public string Chart { get => _chart; set => SetProperty(ref _chart, value); }

	/// <summary>The fix use's token, e.g. <c>WYPNT</c>.</summary>
	public string FixUse { get => _fixUse; set => SetProperty(ref _fixUse, value); }

	/// <summary>The display label, e.g. <c>ENROUTE LOW + WYPNT</c>.</summary>
	public string Label { get => _label; set => SetProperty(ref _label, value); }

	/// <summary>The files this combination writes, for display, e.g. <c>Fix_ENROUTE-LOW-WYPNT_Symbols / _Text</c>.</summary>
	public string FileNames { get => _fileNames; set => SetProperty(ref _fileNames, value); }

	/// <summary>The combination's group name in its file keys and CRC class name, e.g. <c>ENROUTE-LOW-WYPNT</c>.</summary>
	public string Group => FixOutputFiles.CombinationGroup(Chart, FixUse);
}
