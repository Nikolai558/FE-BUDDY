namespace FeBuddy.Core.Application.Conversions.DatToGeojson.Models;

/// <summary>A progress notification from <c>DatToGeojsonService.Run</c> as it works through the files.</summary>
/// <param name="FileName">The <c>.dat</c> file the message is about, without its folder.</param>
/// <param name="Message">A short status line for the run feed.</param>
/// <param name="IsComplete">Whether this file is finished, converted or not.</param>
public sealed record DatToGeojsonProgress(string FileName, string Message, bool IsComplete);
