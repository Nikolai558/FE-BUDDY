using System.Reflection;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Infrastructure.Nasr;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

namespace FeBuddy.UnitTests.Infrastructure.Nasr.Parsers;

/// <summary>
/// Covers parsing only some NASR groups: <see cref="NasrCsvParser.ParseAsync"/> fills just the groups
/// asked for, the AIRAC cycle cache parses exactly <see cref="NasrCsvGroups.InUse"/>, and reading a
/// group that wasn't parsed names it in the error.
/// </summary>
public sealed class NasrCsvGroupParseTests
{
	private static readonly string FixtureDirectory = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Nasr");

	[Fact]
	public async Task parse_async_fills_only_the_groups_asked_for()
	{
		NasrCsvDataCollection data = await NasrCsvParser.ParseAsync(FixtureDirectory, [NasrCsvGroup.Awy, NasrCsvGroup.ClsArsp, NasrCsvGroup.Awy]);

		Assert.Equal([NasrCsvGroup.Awy, NasrCsvGroup.ClsArsp], data.ParsedGroups());
		Assert.Equal(3, data.Awy!.AwyBase.Count);
		Assert.Equal(3, data.ClsArsp!.ClsArsp.Count);
		Assert.Null(data.Apt);
		Assert.Null(data.Ils);
	}

	[Fact]
	public async Task parse_async_with_no_groups_parses_nothing()
	{
		NasrCsvDataCollection data = await NasrCsvParser.ParseAsync(FixtureDirectory, []);

		Assert.Empty(data.ParsedGroups());
	}

	[Fact]
	public async Task parse_all_async_still_fills_all_24_groups()
	{
		NasrCsvDataCollection data = await NasrCsvParser.ParseAllAsync(FixtureDirectory);

		Assert.Equal(24, NasrCsvGroups.All.Count);
		Assert.Equal(NasrCsvGroups.All, data.ParsedGroups());
	}

	[Fact]
	public async Task a_value_that_is_not_a_group_is_rejected()
	{
		await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => NasrCsvParser.ParseAsync(FixtureDirectory, [(NasrCsvGroup)99]));
		await Assert.ThrowsAsync<ArgumentNullException>(() => NasrCsvParser.ParseAsync(FixtureDirectory, null!));
		Assert.Throws<ArgumentOutOfRangeException>(() => new NasrCsvDataCollection().Group((NasrCsvGroup)99));
	}

	[Fact]
	public async Task the_cycle_cache_parses_exactly_the_groups_in_use()
	{
		NasrCsvDataCollection data = await AiracCycleDataCache.ParseInUseAsync(FixtureDirectory, CancellationToken.None);

		Assert.Equal(NasrCsvGroups.InUse.Order(), data.ParsedGroups());
	}

	[Fact]
	public void the_groups_in_use_are_the_nine_fe_buddy_reads()
	{
		Assert.Equal(
			["APT", "ARB", "AWY", "CLS_ARSP", "DP", "FIX", "FRQ", "NAV", "STAR"],
			NasrCsvGroups.InUse.Select(NasrCsvGroups.Code).Order(StringComparer.Ordinal));
	}

	[Theory]
	[InlineData(NasrCsvGroup.ClsArsp, "CLS_ARSP")]
	[InlineData(NasrCsvGroup.MilOps, "MIL_OPS")]
	[InlineData(NasrCsvGroup.Awos, "AWOS")]
	[InlineData(NasrCsvGroup.Star, "STAR")]
	public void a_groups_code_is_the_start_of_its_file_names(NasrCsvGroup group, string code)
	{
		Assert.Equal(code, NasrCsvGroups.Code(group));
	}

	[Fact]
	public void reading_a_group_that_was_not_parsed_names_it_and_points_at_the_in_use_list()
	{
		NasrCsvDataCollection data = new();

		InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => data.RequireIls());

		Assert.Equal(
			"The NASR ILS data has not been parsed. FE-Buddy parses only the groups in NasrCsvGroups.InUse; to use ILS, add it there.",
			ex.Message);
	}

	[Fact]
	public void a_not_parsed_error_can_say_what_cannot_run_without_the_group()
	{
		InvalidOperationException ex = NasrCsvGroups.NotParsed(NasrCsvGroup.Dp, "The Departures sub-service cannot run without it.");

		Assert.Equal(
			"The NASR DP data has not been parsed. The Departures sub-service cannot run without it. " +
			"FE-Buddy parses only the groups in NasrCsvGroups.InUse; to use DP, add it there.",
			ex.Message);
	}

	/// <summary>Every group's <c>Require…</c> method hands back its group once parsed, and names it when not.</summary>
	[Fact]
	public async Task every_require_method_returns_its_group_or_names_it()
	{
		NasrCsvDataCollection parsed = await NasrCsvParser.ParseAllAsync(FixtureDirectory);
		NasrCsvDataCollection empty = new();

		MethodInfo[] requires = [.. typeof(NasrCsvDataCollection).GetMethods()
			.Where(m => m.Name.StartsWith("Require", StringComparison.Ordinal))];

		Assert.Equal(24, requires.Length);

		foreach (MethodInfo require in requires)
		{
			NasrCsvGroup group = Enum.Parse<NasrCsvGroup>(require.Name["Require".Length..]);
			PropertyInfo property = typeof(NasrCsvDataCollection).GetProperty(group.ToString())!;

			Assert.Same(property.GetValue(parsed), require.Invoke(parsed, null));

			TargetInvocationException thrown = Assert.Throws<TargetInvocationException>(() => require.Invoke(empty, null));
			InvalidOperationException inner = Assert.IsType<InvalidOperationException>(thrown.InnerException);
			Assert.Contains($"NASR {NasrCsvGroups.Code(group)} data", inner.Message, StringComparison.Ordinal);
		}
	}
}
