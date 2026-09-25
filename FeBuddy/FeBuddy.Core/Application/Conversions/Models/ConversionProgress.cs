namespace FeBuddy.Core.Application.Conversions.Models;

/// <summary>A progress notification from a file conversion as it works through its source files.</summary>
/// <param name="FileName">The source file the message is about, without its folder.</param>
/// <param name="Message">A short status line for the run feed.</param>
/// <param name="IsComplete">Whether this file is finished, converted or not.</param>
public sealed record ConversionProgress(string FileName, string Message, bool IsComplete);
