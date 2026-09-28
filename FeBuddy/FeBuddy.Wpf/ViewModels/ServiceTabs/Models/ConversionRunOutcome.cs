namespace FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

/// <summary>How a finished conversion is presented on the Review tab.</summary>
/// <param name="Summary">One line of what the run produced, for the Review tab and the completion toast.</param>
/// <param name="Details">The conversion's results block, with its warnings and routine notices.</param>
/// <param name="FilesWritten">Every file the run wrote.</param>
/// <param name="OutputDirectory">The folder to offer to open, or <see langword="null"/> when nothing was written.</param>
public sealed record ConversionRunOutcome(
	string Summary,
	SubServiceRunResult Details,
	IReadOnlyList<string> FilesWritten,
	string? OutputDirectory);
