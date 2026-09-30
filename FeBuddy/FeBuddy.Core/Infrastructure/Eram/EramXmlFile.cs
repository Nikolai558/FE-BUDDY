using System.Xml;

namespace FeBuddy.Core.Infrastructure.Eram;

/// <summary>What the ERAM readers share: how an adaptation export's XML is read, and telling its files apart.</summary>
internal static class EramXmlFile
{
	/// <summary>Comments and whitespace skipped; DTDs refused, as an export never has one.</summary>
	public static XmlReaderSettings Settings => new()
	{
		IgnoreComments = true,
		IgnoreWhitespace = true,
		DtdProcessing = DtdProcessing.Prohibit,
	};

	/// <summary>
	/// Whether a file starts with the given root element, from that element alone - each file of an
	/// adaptation export has its own (<c>Geomaps_Records</c>, <c>ConsoleCommandControl_Records</c>…).
	/// </summary>
	/// <param name="path">The file to look at.</param>
	/// <param name="rootElement">The root element's local name.</param>
	/// <returns><see langword="true"/> when it does; <see langword="false"/> otherwise, or when it cannot be read.</returns>
	public static bool HasRoot(string path, string rootElement)
	{
		try
		{
			using FileStream stream = File.OpenRead(path);
			using XmlReader reader = XmlReader.Create(stream, Settings);

			// MoveToContent lands on the root element or throws, so its name is all there is to check.
			reader.MoveToContent();
			return reader.LocalName == rootElement;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException)
		{
			return false;
		}
	}
}
