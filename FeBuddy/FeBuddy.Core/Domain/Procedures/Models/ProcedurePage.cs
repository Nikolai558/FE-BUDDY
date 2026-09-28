namespace FeBuddy.Core.Domain.Procedures.Models;

/// <summary>
/// One page of a procedure exactly as the d-TPP Metafile lists it: one <c>record</c> element - the
/// main chart or one of its continuation pages.
/// </summary>
/// <param name="ChartName">The chart name exactly as published, e.g. <c>ILS OR LOC RWY 28C</c> or <c>GRUUB ONE (RNAV), CONT.1</c>.</param>
/// <param name="PdfName">The chart's PDF file name, or a deletion placeholder (see <c>DtppFiles</c>) when the page was deleted this cycle.</param>
/// <param name="UserAction">The page's own <c>useraction</c>: <c>"A"</c>, <c>"C"</c>, <c>"D"</c>, or <see langword="null"/> when unchanged.</param>
public sealed record ProcedurePage(string ChartName, string PdfName, string? UserAction);
