using FeBuddy.Core.Application.Airac.Fixes;
using FeBuddy.Core.Application.Airac.Fixes.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Fixes.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Fixes.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Fixes;

/// <summary>
/// Covers <see cref="FixBuilder.Read"/>: the missing-data guard, the blank FIX_ID skip, an
/// unrecognized fix use still being built, duplicate identifiers never being merged, and the
/// stable ordering by identifier.
/// </summary>
public sealed class FixBuilderTests
{
	[Fact]
	public void read_throws_when_fix_data_was_never_parsed()
	{
		NasrCsvDataCollection data = new(); // Fix is null

		Assert.Throws<InvalidOperationException>(() => FixBuilder.Read(data));
	}

	[Fact]
	public void read_rejects_a_null_argument() =>
		Assert.Throws<ArgumentNullException>(() => FixBuilder.Read(null!));

	[Fact]
	public void a_blank_fix_id_is_skipped_with_a_warning()
	{
		NasrCsvDataCollection data = FixTestData.Build([FixTestData.AcmeRow(), FixTestData.BlankFixIdRow()]);

		FixBuildAllResult result = FixBuilder.Read(data);

		Fix fix = Assert.Single(result.Fixes);
		Assert.Equal("ACME", fix.FixId);

		ServiceMessage message = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Warning, message.Level);
		Assert.Equal("FixBuilder", message.Source);
		Assert.Contains("FIX_ID", message.Text);
	}

	[Fact]
	public void a_literal_null_fix_id_is_skipped_with_a_warning_the_same_as_a_blank_one()
	{
		NasrCsvDataCollection data = FixTestData.Build([FixTestData.AcmeRow(), FixTestData.NullFixIdRow()]);

		FixBuildAllResult result = FixBuilder.Read(data);

		Fix fix = Assert.Single(result.Fixes);
		Assert.Equal("ACME", fix.FixId);

		ServiceMessage message = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Warning, message.Level);
	}

	[Fact]
	public void a_literal_null_fix_use_code_is_treated_as_blank()
	{
		NasrCsvDataCollection data = FixTestData.Build([FixTestData.NullFixUseCodeRow("NULLUSE")]);

		Fix fix = Assert.Single(FixBuilder.Read(data).Fixes);

		Assert.Equal(string.Empty, fix.FixUseCode);
		Assert.Equal("UNKNOWN", fix.FixUse);
	}

	[Fact]
	public void every_blank_fix_id_produces_its_own_warning()
	{
		NasrCsvDataCollection data = FixTestData.Build([FixTestData.BlankFixIdRow(), FixTestData.Row("   ")]);

		FixBuildAllResult result = FixBuilder.Read(data);

		Assert.Empty(result.Fixes);
		Assert.Equal(2, result.Messages.Count);
		Assert.All(result.Messages, m => Assert.Equal(LogLevel.Warning, m.Level));
	}

	[Fact]
	public void an_unrecognized_fix_use_code_is_still_built_with_its_mapped_name_kept_literally()
	{
		NasrCsvDataCollection data = FixTestData.Build([FixTestData.ZuluxRow()]);

		Fix fix = Assert.Single(FixBuilder.Read(data).Fixes);

		Assert.Equal("ZQ", fix.FixUseCode);
		Assert.Equal("ZQ", fix.FixUse);
	}

	[Fact]
	public void duplicate_identifiers_are_never_merged()
	{
		NasrCsvDataCollection data = FixTestData.Build([FixTestData.DupexOneRow(), FixTestData.DupexTwoRow()]);

		FixBuildAllResult result = FixBuilder.Read(data);

		Assert.Equal(2, result.Fixes.Count);
		Assert.All(result.Fixes, fix => Assert.Equal("DUPEX", fix.FixId));
		Assert.Contains(result.Fixes, fix => fix.FixUse == "WYPNT");
		Assert.Contains(result.Fixes, fix => fix.FixUse == "RADAR");
	}

	[Fact]
	public void fixes_are_ordered_by_identifier_ignoring_case_stably()
	{
		NasrCsvDataCollection data = FixTestData.Build(
		[
			FixTestData.Row("bbb", "WP"),
			FixTestData.Row("AAA", "CN"),
			FixTestData.Row("aaa", "RP"), // same id as "AAA" ignoring case - must sort right after it, not before.
		]);

		FixBuildAllResult result = FixBuilder.Read(data);

		Assert.Equal(
			[("AAA", "COMPUTER-NAV"), ("aaa", "RPRTNG-PNT"), ("bbb", "WYPNT")],
			result.Fixes.Select(f => (f.FixId, f.FixUse)));
	}

	[Fact]
	public void every_field_is_carried_onto_the_built_fix()
	{
		NasrCsvDataCollection data = FixTestData.Build([FixTestData.AcmeRow()]);

		Fix fix = Assert.Single(FixBuilder.Read(data).Fixes);

		Assert.Equal("ACME", fix.FixId);
		Assert.Equal(40.0, fix.Latitude);
		Assert.Equal(-100.0, fix.Longitude);
		Assert.Equal("WP", fix.FixUseCode);
		Assert.Equal("WYPNT", fix.FixUse);
		Assert.Equal(["ENROUTE LOW", "ENROUTE HIGH"], fix.Charts);
	}

	[Fact]
	public void a_fix_with_no_charts_gets_an_empty_charts_list()
	{
		NasrCsvDataCollection data = FixTestData.Build([FixTestData.BravoRow()]);

		Fix fix = Assert.Single(FixBuilder.Read(data).Fixes);

		Assert.Empty(fix.Charts);
	}

	[Fact]
	public void the_fix_use_code_is_trimmed_before_storing_and_mapping()
	{
		NasrCsvDataCollection data = FixTestData.Build([FixTestData.Row("TRIM", "  wp  ")]);

		Fix fix = Assert.Single(FixBuilder.Read(data).Fixes);

		// Trimmed but not upper-cased for the raw code - only the mapped name is normalized.
		Assert.Equal("wp", fix.FixUseCode);
		Assert.Equal("WYPNT", fix.FixUse);
	}
}
