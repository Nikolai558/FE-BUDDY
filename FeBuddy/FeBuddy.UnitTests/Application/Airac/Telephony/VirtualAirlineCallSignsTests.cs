using FeBuddy.Core.Application.Airac.Telephony;
using FeBuddy.Core.Application.Airac.Telephony.Models;

namespace FeBuddy.UnitTests.Application.Airac.Telephony;

/// <summary>
/// Covers <see cref="VirtualAirlineCallSigns"/>: the virtual airline list looked up by 3LD and
/// telephony, answering as <see cref="VatsimRadarVirtualAirlines.FindSame"/> and
/// <see cref="VatsimRadarVirtualAirlines.FindSameCallSign"/> do, without a pass over the list.
/// </summary>
public sealed class VirtualAirlineCallSignsTests
{
	private static readonly VirtualAirline Delta = new("DVA", "DELTA", "Delta Virtual");
	private static readonly VirtualAirline FlyDelta = new(" dva ", "Delta", "Fly Delta Virtual");
	private static readonly VirtualAirline United = new("UAV", "UNITED", "United Virtual");

	/// <summary>Every airline with the 3LD and telephony, trimmed and ignoring case, in the list's order.</summary>
	[Fact]
	public void the_list_is_looked_up_by_3ld_and_telephony()
	{
		VirtualAirlineCallSigns callSigns = VirtualAirlineCallSigns.Build([Delta, United, FlyDelta]);

		Assert.Equal([Delta, FlyDelta], callSigns.WithCallSign("dva", " delta "));
		Assert.Same(Delta, callSigns.FindSameCallSign("DVA", "DELTA"));
		Assert.Same(FlyDelta, callSigns.FindSame("DVA", "DELTA", " fly delta virtual "));
		Assert.Null(callSigns.FindSame("DVA", "DELTA", "Another Virtual"));
		Assert.Empty(callSigns.WithCallSign("DVA", "DELTAX"));
		Assert.Null(callSigns.FindSameCallSign("XXX", "DELTA"));
	}

	/// <summary>It answers as the list's own searches do.</summary>
	[Fact]
	public void it_answers_as_the_lists_own_searches()
	{
		VirtualAirline[] list = [Delta, United, FlyDelta];
		VirtualAirlineCallSigns callSigns = VirtualAirlineCallSigns.Build(list);

		foreach (VirtualAirline asked in list.Append(new VirtualAirline("DVA", "DELTA", "Nobody")))
		{
			Assert.Same(VatsimRadarVirtualAirlines.FindSameCallSign(list, asked.Designator, asked.Telephony), callSigns.FindSameCallSign(asked.Designator, asked.Telephony));
			Assert.Same(VatsimRadarVirtualAirlines.FindSame(list, asked.Designator, asked.Telephony, asked.Organization),
				callSigns.FindSame(asked.Designator, asked.Telephony, asked.Organization));
		}
	}

	/// <summary>A list's lookup is built once and kept with it; no list finds nothing.</summary>
	[Fact]
	public void a_lists_lookup_is_built_once()
	{
		VirtualAirline[] list = [Delta];

		Assert.Same(VirtualAirlineCallSigns.For(list), VirtualAirlineCallSigns.For(list));
		Assert.NotSame(VirtualAirlineCallSigns.For(list), VirtualAirlineCallSigns.For([Delta]));
		Assert.Empty(VirtualAirlineCallSigns.Empty.WithCallSign("DVA", "DELTA"));
	}

	/// <summary>Tens of thousands of airlines are looked up at once.</summary>
	[Fact]
	public void tens_of_thousands_are_looked_up_at_once()
	{
		VirtualAirline[] list = [.. Enumerable.Range(0, 50_000).Select(i => new VirtualAirline($"V{i % 26:D2}", $"CALL{i}", $"Org {i}"))];
		System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

		VirtualAirlineCallSigns callSigns = VirtualAirlineCallSigns.Build(list);

		for (int i = 0; i < 50_000; i += 7)
		{
			Assert.NotNull(callSigns.FindSame($"V{i % 26:D2}", $"CALL{i}", $"Org {i}"));
		}

		Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(1), $"Took {stopwatch.Elapsed}.");
	}

	/// <summary>Missing arguments throw.</summary>
	[Fact]
	public void missing_arguments_throw()
	{
		VirtualAirlineCallSigns callSigns = VirtualAirlineCallSigns.Build([Delta]);

		Assert.Throws<ArgumentNullException>(() => VirtualAirlineCallSigns.For(null!));
		Assert.Throws<ArgumentNullException>(() => VirtualAirlineCallSigns.Build(null!));
		Assert.Throws<ArgumentNullException>(() => callSigns.WithCallSign(null!, "DELTA"));
		Assert.Throws<ArgumentNullException>(() => callSigns.WithCallSign("DVA", null!));
		Assert.Throws<ArgumentNullException>(() => callSigns.FindSame("DVA", "DELTA", null!));
	}
}
