using FeBuddy.Core.Application.Airac.Telephony;
using FeBuddy.Core.Application.Airac.Telephony.Models;
using FeBuddy.Core.Domain.Telephony.Models;
using FeBuddy.Core.Infrastructure.Telephony.Models;

namespace FeBuddy.UnitTests.Application.Airac.Telephony;

/// <summary>
/// Covers <see cref="TelephonyBuilder"/>: which register rows and U.S. special call signs get a
/// card versus are counted as left out (no designator, no telephony, expired), the two date formats
/// the FAA prints an expiration in, an unreadable expiration being kept with a warning, every value
/// upper-cased and trimmed into <see cref="TelephonyEntry"/>, entry order (assignments, then special
/// call signs, then the user's virtual airlines), and the null-argument check.
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
}
