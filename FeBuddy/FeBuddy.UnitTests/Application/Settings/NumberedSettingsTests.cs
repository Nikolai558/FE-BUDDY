using FeBuddy.Core.Application.Settings;

namespace FeBuddy.UnitTests.Application.Settings;

/// <summary>Covers <see cref="NumberedSettings"/>: a numbered list's keys, read the same way by the parsers and the tabs.</summary>
public sealed class NumberedSettingsTests
{
	/// <summary>Only <c>&lt;Prefix&gt;&lt;n&gt;.&lt;Field&gt;</c> with a number from 1 is one of the list's keys; the prefix ignores case.</summary>
	[Theory]
	[InlineData("Sources.2.Url", 2, "Url")]
	[InlineData("sources.10.FilePath", 10, "FilePath")]
	[InlineData("Sources.0.Url", 0, null)]
	[InlineData("Sources.-1.Url", 0, null)]
	[InlineData("Sources.x.Url", 0, null)]
	[InlineData("Sources.2", 0, null)]
	[InlineData("Sources.2.Url.More", 0, null)]
	[InlineData("Sources.2.", 0, null)]
	[InlineData("Other.2.Url", 0, null)]
	public void a_key_is_split_into_its_number_and_field(string key, int number, string? field)
	{
		bool split = NumberedSettings.TrySplit(key, "Sources.", out int gotNumber, out string gotField);

		Assert.Equal(field is not null, split);
		Assert.Equal(number, gotNumber);
		Assert.Equal(field ?? string.Empty, gotField);
	}

	/// <summary>The entries come back in number order, each field trimmed, and the keys read are named.</summary>
	[Fact]
	public void the_entries_are_read_in_number_order()
	{
		Dictionary<string, string> values = new()
		{
			["Node.Sources.10.Url"] = " https://b ",
			["Node.Sources.2.FilePath"] = "C:\\a.txt",
			["Node.Sources.2.Url"] = "",
			["Node.Other"] = "x",
			["Node.Sources.0.Url"] = "dropped",
		};
		HashSet<string> read = [];

		SortedDictionary<int, Dictionary<string, string>> entries = NumberedSettings.Read(values, "Node.Sources.", read);

		Assert.Equal([2, 10], entries.Keys);
		Assert.Equal("C:\\a.txt", entries[2]["filepath"]);
		Assert.Equal(string.Empty, entries[2]["Url"]);
		Assert.Equal("https://b", entries[10]["Url"]);
		Assert.Equal(3, read.Count);
		Assert.Throws<ArgumentNullException>(() => NumberedSettings.Read(null!, "x."));
		Assert.Throws<ArgumentNullException>(() => NumberedSettings.TrySplit(null!, "x.", out _, out _));
	}
}
