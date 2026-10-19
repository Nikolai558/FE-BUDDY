using FeBuddy.Core.Application.Models;

namespace FeBuddy.UnitTests.Application.Models;

/// <summary>Covers <see cref="FileNameExtensions"/>: every extension a file FE-Buddy reads or writes has, from the services' own lists.</summary>
public sealed class FileNameExtensionsTests
{
	[Fact]
	public void every_format_is_there_once_longest_first()
	{
		Assert.Equal(
			[".geojson", ".json", ".sct2", ".csv", ".dat", ".log", ".sct", ".txt", ".xml", ".md"],
			FileNameExtensions.All);
	}
}
