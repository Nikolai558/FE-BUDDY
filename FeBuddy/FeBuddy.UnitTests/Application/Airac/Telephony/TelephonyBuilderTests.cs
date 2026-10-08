using FeBuddy.Core.Application.Airac.Telephony;
using FeBuddy.Core.Application.Airac.Telephony.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Telephony.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Telephony.Models;

namespace FeBuddy.UnitTests.Application.Airac.Telephony;

/// <summary>
/// Covers <see cref="TelephonyBuilder"/>: which register rows and U.S. special call signs get a
/// card versus are counted as left out (no designator, no telephony, expired), the two date formats
/// the FAA prints an expiration in, an unreadable expiration being kept with a warning, every value
/// upper-cased and trimmed into <see cref="TelephonyEntry"/>, entry order (assignments, then special
/// call signs, then the user's virtual airlines, then the virtual airline list's), and the null-argument
/// check - plus the virtual airline list: left out unless included, and entries that could not be a
/// virtual airline left out and named. And only the first entry for each 3LD and telephony written
/// (issues #336, #339): a real operator's over any virtual airline, the user's own over the list's.
/// </summary>
public sealed class TelephonyBuilderTests
{
	private static readonly DateOnly Today = new(2026, 9, 27);

	private static TelephonyHtmlDataModel.Assignment Assignment(string company, string country, string telephony, string designator) => new()
	{
		Company = company,
		Country = country,
		Telephony = telephony,
		ThreeLetterDesignator = designator,
	};

	private static TelephonyHtmlDataModel.SpecialCallSign SpecialCallSign(string telephony, string identifier, string agency, string expiration) => new()
	{
		Telephony = telephony,
		Identifier = identifier,
		Agency = agency,
		ExpirationDate = expiration,
	};

	private static TelephonyDataCollection Data(
		IEnumerable<TelephonyHtmlDataModel.Assignment>? assignments = null,
		IEnumerable<TelephonyHtmlDataModel.SpecialCallSign>? specialCallSigns = null) => new()
		{
			Assignments = [.. assignments ?? []],
			SpecialCallSigns = [.. specialCallSigns ?? []],
		};

	// ---- register rows: no designator / no telephony ----

	[Theory]
	[InlineData("...")]
	[InlineData("--")]
	[InlineData("")]
	public void a_register_row_with_no_designator_is_left_out_and_counted(string designator)
	{
		TelephonyDataCollection data = Data([Assignment("SOME COMPANY", "UNITED STATES", "SOME TELEPHONY", designator)]);

		TelephonyBuildResult result = TelephonyBuilder.Read(data, Today);

		Assert.Empty(result.Entries);
		Assert.Equal(1, result.NoDesignatorCount);
		Assert.Equal(0, result.NoTelephonyCount);
	}

	[Fact]
	public void a_register_row_with_a_designator_but_no_telephony_is_left_out_and_counted()
	{
		TelephonyDataCollection data = Data([Assignment("SOME COMPANY", "UNITED STATES", "", "ABC")]);

		TelephonyBuildResult result = TelephonyBuilder.Read(data, Today);

		Assert.Empty(result.Entries);
		Assert.Equal(0, result.NoDesignatorCount);
		Assert.Equal(1, result.NoTelephonyCount);
	}

	// ---- special call signs: no identifier / no telephony ----

	[Fact]
	public void a_special_call_sign_with_a_blank_identifier_is_left_out_and_counted_as_no_designator()
	{
		TelephonyDataCollection data = Data(specialCallSigns: [SpecialCallSign("AIR SIX", "", "SOME AGENCY", "N/A")]);

		TelephonyBuildResult result = TelephonyBuilder.Read(data, Today);

		Assert.Empty(result.Entries);
		Assert.Equal(1, result.NoDesignatorCount);
	}

	[Fact]
	public void a_special_call_sign_with_no_telephony_is_left_out_and_counted()
	{
		TelephonyDataCollection data = Data(specialCallSigns: [SpecialCallSign("", "ARSIX", "SOME AGENCY", "N/A")]);

		TelephonyBuildResult result = TelephonyBuilder.Read(data, Today);

		Assert.Empty(result.Entries);
		Assert.Equal(0, result.NoDesignatorCount);
		Assert.Equal(1, result.NoTelephonyCount);
	}

	// ---- expiration ----

	[Fact]
	public void a_special_call_sign_expired_before_today_is_left_out_and_counted()
	{
		TelephonyDataCollection data = Data(specialCallSigns: [SpecialCallSign("AIR SIX", "ARSIX", "SOME AGENCY", "1-Jan-2020")]);

		TelephonyBuildResult result = TelephonyBuilder.Read(data, Today);

		Assert.Empty(result.Entries);
		Assert.Equal(1, result.ExpiredCount);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void a_special_call_sign_expiring_exactly_today_is_kept()
	{
		TelephonyDataCollection data = Data(specialCallSigns: [SpecialCallSign("AIR SIX", "ARSIX", "SOME AGENCY", "27-Sep-2026")]);

		TelephonyBuildResult result = TelephonyBuilder.Read(data, Today);

		Assert.Single(result.Entries);
		Assert.Equal(0, result.ExpiredCount);
	}

	[Theory]
	[InlineData("N/A")]
	[InlineData("")]
	public void a_special_call_sign_with_no_expiration_is_kept(string expiration)
	{
		TelephonyDataCollection data = Data(specialCallSigns: [SpecialCallSign("AIR SIX", "ARSIX", "SOME AGENCY", expiration)]);

		TelephonyBuildResult result = TelephonyBuilder.Read(data, Today);

		Assert.Single(result.Entries);
		Assert.Equal(0, result.ExpiredCount);
		Assert.Empty(result.Messages);
	}

	[Theory]
	[InlineData("4-Nov-2029")]
	[InlineData("24-Feb-2027")]
	public void both_faa_expiration_date_formats_parse_and_are_kept_when_not_yet_expired(string expiration)
	{
		TelephonyDataCollection data = Data(specialCallSigns: [SpecialCallSign("AIR SIX", "ARSIX", "SOME AGENCY", expiration)]);

		TelephonyBuildResult result = TelephonyBuilder.Read(data, Today);

		Assert.Single(result.Entries);
		Assert.Equal(0, result.ExpiredCount);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void an_unreadable_expiration_date_is_kept_with_a_warning_naming_it()
	{
		TelephonyDataCollection data = Data(specialCallSigns: [SpecialCallSign("AIR SIX", "ARSIX", "SOME AGENCY", "SOMEDAY")]);

		TelephonyBuildResult result = TelephonyBuilder.Read(data, Today);

		Assert.Single(result.Entries);
		Assert.Equal(0, result.ExpiredCount);

		string warning = Assert.Single(result.Messages.WarningTexts());
		Assert.Contains("AIR SIX", warning);
		Assert.Contains("ARSIX", warning);
		Assert.Contains("SOMEDAY", warning);
	}

	// ---- values upper-cased & trimmed ----

	[Fact]
	public void a_register_row_is_upper_cased_and_trimmed_into_an_icao_assignment_entry()
	{
		TelephonyDataCollection data = Data([Assignment("  avianca s.a.  ", " colombia ", " avianca ", " ava ")]);

		TelephonyEntry entry = Assert.Single(TelephonyBuilder.Read(data, Today).Entries);

		Assert.Equal(TelephonyEntryKind.IcaoAssignment, entry.Kind);
		Assert.Equal("AVA", entry.Identifier);
		Assert.Equal("AVIANCA", entry.Telephony);
		Assert.Equal("AVIANCA S.A.", entry.Organization);
		Assert.Equal("COLOMBIA", entry.Detail);
	}

	[Fact]
	public void a_special_call_sign_is_upper_cased_and_trimmed_into_a_us_special_call_sign_entry()
	{
		TelephonyDataCollection data = Data(specialCallSigns: [SpecialCallSign(" air six ", " arsix ", " some agency ", " 24-feb-2027 ")]);

		TelephonyEntry entry = Assert.Single(TelephonyBuilder.Read(data, Today).Entries);

		Assert.Equal(TelephonyEntryKind.UsSpecialCallSign, entry.Kind);
		Assert.Equal("ARSIX", entry.Identifier);
		Assert.Equal("AIR SIX", entry.Telephony);
		Assert.Equal("SOME AGENCY", entry.Organization);
		Assert.Equal("24-FEB-2027", entry.Detail);
	}

	// ---- entry order ----

	[Fact]
	public void entries_list_assignments_before_special_call_signs()
	{
		TelephonyDataCollection data = Data(
			[Assignment("SOME COMPANY", "UNITED STATES", "SOME TELEPHONY", "ABC")],
			[SpecialCallSign("AIR SIX", "ARSIX", "SOME AGENCY", "N/A")]);

		TelephonyBuildResult result = TelephonyBuilder.Read(data, Today);

		Assert.Equal(2, result.Entries.Count);
		Assert.Equal(TelephonyEntryKind.IcaoAssignment, result.Entries[0].Kind);
		Assert.Equal(TelephonyEntryKind.UsSpecialCallSign, result.Entries[1].Kind);
	}

	[Fact]
	public void read_rejects_a_null_data_argument() =>
		Assert.Throws<ArgumentNullException>(() => TelephonyBuilder.Read(null!, Today));

	// ---- virtual airlines ----

	[Fact]
	public void virtual_airlines_come_after_every_faa_entry_in_list_order()
	{
		TelephonyDataCollection data = Data(
			[Assignment("SOME COMPANY", "UNITED STATES", "SOME TELEPHONY", "ABC")],
			[SpecialCallSign("AIR SIX", "ARSIX", "SOME AGENCY", "N/A")]);
		VirtualAirline[] virtualAirlines =
		[
			new("ZZV", "ZULU", "Zulu Virtual"),
			new("AAV", "ALPHA", "Alpha Virtual"),
		];

		TelephonyBuildResult result = TelephonyBuilder.Read(data, Today, virtualAirlines);

		Assert.Equal(
			[TelephonyEntryKind.IcaoAssignment, TelephonyEntryKind.UsSpecialCallSign, TelephonyEntryKind.VirtualAirline, TelephonyEntryKind.VirtualAirline],
			result.Entries.Select(entry => entry.Kind));
		Assert.Equal(["ABC", "ARSIX", "ZZV", "AAV"], result.Entries.Select(entry => entry.Identifier));
	}

	[Fact]
	public void a_virtual_airline_is_upper_cased_and_trimmed_into_an_entry_with_no_detail()
	{
		TelephonyDataCollection data = Data();

		TelephonyEntry entry = Assert.Single(
			TelephonyBuilder.Read(data, Today, [new VirtualAirline(" dva ", " delta ", " Delta Virtual ")]).Entries);

		Assert.Equal(TelephonyEntryKind.VirtualAirline, entry.Kind);
		Assert.Equal("DVA", entry.Identifier);
		Assert.Equal("DELTA", entry.Telephony);
		Assert.Equal("DELTA VIRTUAL", entry.Organization);
		Assert.Equal(string.Empty, entry.Detail);
	}

	/// <summary>A virtual airline is never a row the FAA left out, so it is in none of the left-out counts and gives no message.</summary>
	[Fact]
	public void virtual_airlines_are_not_counted_as_left_out_and_add_no_messages()
	{
		TelephonyBuildResult result = TelephonyBuilder.Read(Data(), Today, [new VirtualAirline("DVA", "DELTA", "Delta Virtual")]);

		Assert.Equal(0, result.NoDesignatorCount);
		Assert.Equal(0, result.NoTelephonyCount);
		Assert.Equal(0, result.ExpiredCount);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void no_virtual_airlines_null_or_omitted_adds_none()
	{
		TelephonyDataCollection data = Data([Assignment("SOME COMPANY", "UNITED STATES", "SOME TELEPHONY", "ABC")]);

		Assert.Single(TelephonyBuilder.Read(data, Today).Entries);
		Assert.Single(TelephonyBuilder.Read(data, Today, null).Entries);
		Assert.Single(TelephonyBuilder.Read(data, Today, []).Entries);
		Assert.DoesNotContain(TelephonyBuilder.Read(data, Today, null).Entries, entry => entry.Kind == TelephonyEntryKind.VirtualAirline);
	}

	// ---- the virtual airline list ----

	private static TelephonyDataCollection DataWithList(params VatsimRadarAirline[] list)
	{
		TelephonyDataCollection data = Data([Assignment("DELTA AIR LINES, INC.", "UNITED STATES", "DELTA", "DAL")]);
		data.VatsimRadarAirlines = [.. list];
		return data;
	}

	private static string[] Cards(TelephonyBuildResult result) =>
		[.. result.Entries.Select(entry => $"{entry.Identifier} / {entry.Telephony} / {entry.Organization}")];

	[Fact]
	public void the_lists_virtual_airlines_come_after_yours_sorted_and_upper_cased()
	{
		TelephonyDataCollection data = DataWithList(
			new VatsimRadarAirline("OCN", "vOCN", "Ocean"),
			new VatsimRadarAirline("ASK", "Airsky", "Airsky"));

		TelephonyBuildResult result = TelephonyBuilder.Read(data, Today, [new VirtualAirline("ZZV", "ZULU", "Zulu Virtual")], includeVatsimRadar: true);

		Assert.Equal(
			["DAL / DELTA / DELTA AIR LINES, INC.", "ZZV / ZULU / ZULU VIRTUAL", "ASK / AIRSKY / AIRSKY", "OCN / OCEAN / VOCN"],
			Cards(result));
		Assert.Equal(2, result.VatsimRadarVirtualAirlineCount);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void the_list_is_left_out_unless_included()
	{
		TelephonyDataCollection data = DataWithList(new VatsimRadarAirline("OCN", "vOCN", "Ocean"));

		TelephonyBuildResult result = TelephonyBuilder.Read(data, Today);

		Assert.DoesNotContain(result.Entries, entry => entry.Kind == TelephonyEntryKind.VirtualAirline);
		Assert.Equal(0, result.VatsimRadarVirtualAirlineCount);
	}

	/// <summary>
	/// Issues #336 and #339: a virtual airline with a real operator's 3LD and telephony - whatever its
	/// virtual organization, and whether it is the user's own or the list's - is left out, and named.
	/// </summary>
	[Fact]
	public void a_virtual_airline_with_a_real_operators_3ld_and_telephony_is_left_out_and_named()
	{
		TelephonyDataCollection data = Data(
		[
			Assignment("AMERICAN AIRLINES INC.", "UNITED STATES", "AMERICAN", "AAL"),
			Assignment("VIRGIN ATLANTIC AIRWAYS LTD", "UNITED KINGDOM", "VIRGIN", "VIR"),
		]);
		data.VatsimRadarAirlines = [new VatsimRadarAirline("AAL", "American Virtual", "American"), new VatsimRadarAirline("VIR", "VRGN Virtual", "Virgin")];

		TelephonyBuildResult result = TelephonyBuilder.Read(data, Today, [new VirtualAirline("aal", "american", "My American")], includeVatsimRadar: true);

		Assert.Equal(
			["AAL / AMERICAN / AMERICAN AIRLINES INC.", "VIR / VIRGIN / VIRGIN ATLANTIC AIRWAYS LTD"],
			Cards(result));
		Assert.Equal(0, result.VatsimRadarVirtualAirlineCount);
		Assert.Equal(
			[
				"1 of your virtual airlines has the same 3LD and telephony as a real operator, so only the real operator was written: " +
					"AAL AMERICAN (MY AMERICAN).",
				"2 virtual airline(s) on the Virtual Airline List (GNG + VATSIM-Radar) have the same 3LD and telephony as a real operator, " +
					"so only the real operator was written: AAL AMERICAN (AMERICAN VIRTUAL), VIR VIRGIN (VRGN VIRTUAL).",
			],
			result.Messages.Select(message => message.Text));
		Assert.All(result.Messages, message => Assert.Equal(LogLevel.Info, message.Level));
	}

	/// <summary>The user's own comes before the list's: one of the list's with the same 3LD and telephony is left out, whatever its organization.</summary>
	[Fact]
	public void one_of_the_lists_with_one_of_yours_3ld_and_telephony_is_left_out_and_yours_is_written()
	{
		TelephonyDataCollection data = DataWithList(
			new VatsimRadarAirline("DVA", "Delta Virtual", "Delta"),
			new VatsimRadarAirline("DVA", "Rustic Virtual", "Delta"));

		TelephonyBuildResult result = TelephonyBuilder.Read(data, Today, [new VirtualAirline("DVA", "DELTA", "My Delta")], includeVatsimRadar: true);

		Assert.Equal(
			["MY DELTA"],
			result.Entries.Where(entry => entry.Kind == TelephonyEntryKind.VirtualAirline).Select(entry => entry.Organization));
		Assert.Equal(0, result.VatsimRadarVirtualAirlineCount);
		Assert.Equal(
			"2 virtual airline(s) on the Virtual Airline List (GNG + VATSIM-Radar) have the same 3LD and telephony as one of yours, " +
			"so only yours was written: DVA DELTA (DELTA VIRTUAL), DVA DELTA (RUSTIC VIRTUAL).",
			Assert.Single(result.Messages).Text);
	}

	/// <summary>GNG lists some 3LDs more than once: of the list's with the same 3LD and telephony, only the first (sorted) is written.</summary>
	[Fact]
	public void of_the_lists_with_the_same_3ld_and_telephony_only_the_first_is_written()
	{
		TelephonyDataCollection data = DataWithList(
			new VatsimRadarAirline("SKA", "Sky Jet", "Skyjet"),
			new VatsimRadarAirline("SKA", "Skyjet Airlines Virtual", "Skyjet"),
			new VatsimRadarAirline("SKA", "Sky Air", "Skyair"));

		TelephonyBuildResult result = TelephonyBuilder.Read(data, Today, includeVatsimRadar: true);

		Assert.Equal(["SKA / SKYAIR / SKY AIR", "SKA / SKYJET / SKY JET"], Cards(result).Skip(1));
		Assert.Equal(2, result.VatsimRadarVirtualAirlineCount);
		Assert.Equal(
			"1 virtual airline(s) on the Virtual Airline List (GNG + VATSIM-Radar) repeat an earlier one's 3LD and telephony, " +
			"so only the first was written: SKA SKYJET (SKYJET AIRLINES VIRTUAL).",
			Assert.Single(result.Messages).Text);
	}

	/// <summary>Two of the user's own with the same 3LD and telephony (a hand-edited config): only the first is written.</summary>
	[Fact]
	public void of_yours_with_the_same_3ld_and_telephony_only_the_first_is_written()
	{
		TelephonyBuildResult result = TelephonyBuilder.Read(
			Data(), Today, [new VirtualAirline("DVA", "DELTA", "Delta Virtual"), new VirtualAirline("DVA", "DELTA", "Rustic Virtual")]);

		Assert.Equal(["DVA / DELTA / DELTA VIRTUAL"], Cards(result));
		Assert.Equal(
			"1 of your virtual airlines has the same 3LD and telephony as another of yours, so only the first was written: DVA DELTA (RUSTIC VIRTUAL).",
			Assert.Single(result.Messages).Text);
	}

	/// <summary>The FAA's own repeats - the same 3LD and telephony, another company or country - are written once too, the first.</summary>
	[Fact]
	public void of_the_faas_rows_with_the_same_3ld_and_telephony_only_the_first_is_written()
	{
		TelephonyDataCollection data = Data(
			[
				Assignment("FIRST COMPANY", "FRANCE", "SAMECALL", "SAM"),
				Assignment("SECOND COMPANY", "SPAIN", "samecall", "sam"),
			],
			[SpecialCallSign("SAMECALL", "SAM", "AN AGENCY", "N/A")]);

		TelephonyBuildResult result = TelephonyBuilder.Read(data, Today);

		Assert.Equal(["SAM / SAMECALL / FIRST COMPANY"], Cards(result));
		Assert.Equal(
			"2 FAA telephony row(s) repeat an earlier row's 3LD and telephony, so only the first was written: " +
			"SAM SAMECALL (SECOND COMPANY), SAM SAMECALL (AN AGENCY).",
			Assert.Single(result.Messages).Text);
	}

	/// <summary>The same 3LD with another telephony - or the same telephony with another 3LD - is another operator, still written.</summary>
	[Fact]
	public void another_telephony_or_another_3ld_is_another_operator()
	{
		TelephonyDataCollection data = DataWithList(
			new VatsimRadarAirline("DAL", "Delta Virtual", "Delta Virtual"),
			new VatsimRadarAirline("DVA", "Delta Virtual", "Delta"));

		TelephonyBuildResult result = TelephonyBuilder.Read(data, Today, includeVatsimRadar: true);

		Assert.Equal(3, result.Entries.Count);
		Assert.Empty(result.Messages);
	}

	/// <summary>A long list of left-out entries names twenty, then says how many more.</summary>
	[Fact]
	public void a_long_list_left_out_names_twenty_then_how_many_more()
	{
		TelephonyDataCollection data = Data([.. Enumerable.Range(0, 25).Select(i => Assignment($"COMPANY {i}", "X", "SAMECALL", "SAM"))]);

		TelephonyBuildResult result = TelephonyBuilder.Read(data, Today);

		string text = Assert.Single(result.Messages).Text;
		Assert.StartsWith("24 FAA telephony row(s) repeat", text, StringComparison.Ordinal);
		Assert.EndsWith("SAM SAMECALL (COMPANY 20), and 4 more.", text, StringComparison.Ordinal);
	}

	[Fact]
	public void a_list_entry_that_could_not_be_a_virtual_airline_of_yours_is_left_out_and_named()
	{
		TelephonyDataCollection data = DataWithList(
			new VatsimRadarAirline("PHENX", "Phoenix AirV", "PHOENIX"),
			new VatsimRadarAirline("C", "United States Coast Guard Virtual", "COAST GUARD"),
			new VatsimRadarAirline("ASK", "AIRSKY", "AIRSKY"));

		TelephonyBuildResult result = TelephonyBuilder.Read(data, Today, includeVatsimRadar: true);

		Assert.Equal(1, result.VatsimRadarVirtualAirlineCount);
		ServiceMessage note = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Info, note.Level);
		Assert.Equal(
			"2 virtual airline(s) on the Virtual Airline List (GNG + VATSIM-Radar) were left out: a 3LD that isn't three letters, " +
			"or a telephony with no letter or digit (C, PHENX).",
			note.Text);
	}
}
