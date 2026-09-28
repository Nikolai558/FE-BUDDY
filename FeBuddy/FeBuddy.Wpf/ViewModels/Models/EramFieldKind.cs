namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>Which CRC ERAM field block a row belongs to.</summary>
public enum EramFieldKind
{
	/// <summary>Line block: bcg, filters, style, thickness.</summary>
	Line,

	/// <summary>Symbol block: bcg, filters, style, size.</summary>
	Symbol,

	/// <summary>Text block: bcg, filters, size, underline, opaque, xOffset, yOffset.</summary>
	Text,
}
