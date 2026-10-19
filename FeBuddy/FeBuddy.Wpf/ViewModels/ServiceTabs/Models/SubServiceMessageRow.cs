namespace FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

/// <summary>
/// One message in a sub-service's expanded list on a run's review, under its group's heading
/// (<see cref="SubServiceMessageGroup"/>). The list shows headings and messages as one flat,
/// virtualized list, so only the rows on screen are built.
/// </summary>
/// <param name="Text">The message.</param>
/// <param name="IsLast">Whether it is its group's last message, which closes the group's panel.</param>
public sealed record SubServiceMessageRow(string Text, bool IsLast);
