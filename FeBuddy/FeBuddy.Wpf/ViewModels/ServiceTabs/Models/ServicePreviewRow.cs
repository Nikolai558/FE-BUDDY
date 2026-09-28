namespace FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

/// <summary>One label / value line on the Preview Settings tab.</summary>
/// <param name="Label">What the setting is called.</param>
/// <param name="Value">Its current value, already formatted for display.</param>
public sealed record ServicePreviewRow(string Label, string Value);
