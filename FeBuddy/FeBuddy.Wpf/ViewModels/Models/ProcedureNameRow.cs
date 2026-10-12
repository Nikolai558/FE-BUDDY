namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One departure or arrival listed on the Procedure Charts tab's Additional Procedures to Include card.
/// </summary>
/// <param name="Name">Its root name, e.g. <c>BRWNZ</c>, as saved.</param>
/// <param name="Detail">
/// What it brings in from the selected cycle, e.g. <c>Cycle 2611: BRWNZ FIVE (RNAV)</c>, or that the
/// cycle has none; <see langword="null"/> until the cycle's metafile is known.
/// </param>
/// <param name="IsMissing">Whether the selected cycle has no departure or arrival by that name, so it brings in nothing.</param>
public sealed record ProcedureNameRow(string Name, string? Detail, bool IsMissing);
