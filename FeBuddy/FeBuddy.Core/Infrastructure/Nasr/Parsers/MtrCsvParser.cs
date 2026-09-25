using static FeBuddy.Core.Infrastructure.Nasr.Models.MtrCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers;

/// <summary>
/// Reads the NASR <c>MTR</c> CSV files (military training routes), one file per method.
/// </summary>
public class MtrCsvParser
{
	/// <summary>Reads <c>MTR_AGY.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="MtrCsvDataCollection.MtrAgy"/> filled in.</returns>
	public MtrCsvDataCollection ParseMtrAgy(string filePath)
	{
		var result = new MtrCsvDataCollection
		{
			MtrAgy = NasrCsvReader.ProcessLines(
				filePath,
				fields => new MtrAgy
				{
					EffDate = fields["EFF_DATE"],
					RouteTypeCode = fields["ROUTE_TYPE_CODE"],
					RouteId = fields["ROUTE_ID"],
					Artcc = fields["ARTCC"],
					AgencyType = fields["AGENCY_TYPE"],
					AgencyName = fields["AGENCY_NAME"],
					Station = fields["STATION"],
					Address = fields["ADDRESS"],
					City = fields["CITY"],
					StateCode = fields["STATE_CODE"],
					ZipCode = fields["ZIP_CODE"],
					CommercialNo = fields["COMMERCIAL_NO"],
					DsnNo = fields["DSN_NO"],
					Hours = fields["HOURS"],
				})
		};

		return result;
	}

	/// <summary>Reads <c>MTR_BASE.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="MtrCsvDataCollection.MtrBase"/> filled in.</returns>
	public MtrCsvDataCollection ParseMtrBase(string filePath)
	{
		var result = new MtrCsvDataCollection
		{
			MtrBase = NasrCsvReader.ProcessLines(
				filePath,
				fields => new MtrBase
				{
					EffDate = fields["EFF_DATE"],
					RouteTypeCode = fields["ROUTE_TYPE_CODE"],
					RouteId = fields["ROUTE_ID"],
					Artcc = fields["ARTCC"],
					Fss = fields["FSS"],
					TimeOfUse = fields["TIME_OF_USE"],
				})
		};

		return result;
	}

	/// <summary>Reads <c>MTR_PT.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="MtrCsvDataCollection.MtrPt"/> filled in.</returns>
	public MtrCsvDataCollection ParseMtrPt(string filePath)
	{
		var result = new MtrCsvDataCollection
		{
			MtrPt = NasrCsvReader.ProcessLines(
				filePath,
				fields => new MtrPt
				{
					EffDate = fields["EFF_DATE"],
					RouteTypeCode = fields["ROUTE_TYPE_CODE"],
					RouteId = fields["ROUTE_ID"],
					Artcc = fields["ARTCC"],
					RoutePtSeq = NasrCsvReader.ParseInt(fields["ROUTE_PT_SEQ"]),
					RoutePtId = fields["ROUTE_PT_ID"],
					NextRoutePtId = fields["NEXT_ROUTE_PT_ID"],
					SegmentText = fields["SEGMENT_TEXT"],
					LatDeg = NasrCsvReader.ParseInt(fields["LAT_DEG"]),
					LatMin = NasrCsvReader.ParseInt(fields["LAT_MIN"]),
					LatSec = NasrCsvReader.ParseDouble(fields["LAT_SEC"]),
					LatHemis = fields["LAT_HEMIS"],
					LatDecimal = NasrCsvReader.ParseDouble(fields["LAT_DECIMAL"]),
					LongDeg = NasrCsvReader.ParseInt(fields["LONG_DEG"]),
					LongMin = NasrCsvReader.ParseInt(fields["LONG_MIN"]),
					LongSec = NasrCsvReader.ParseDouble(fields["LONG_SEC"]),
					LongHemis = fields["LONG_HEMIS"],
					LongDecimal = NasrCsvReader.ParseDouble(fields["LONG_DECIMAL"]),
					NavId = fields["NAV_ID"],
					NavaidBearing = NasrCsvReader.ParseNullableInt(fields["NAVAID_BEARING"]),
					NavaidDist = NasrCsvReader.ParseNullableInt(fields["NAVAID_DIST"]),
				})
		};

		return result;
	}

	/// <summary>Reads <c>MTR_SOP.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="MtrCsvDataCollection.MtrSop"/> filled in.</returns>
	public MtrCsvDataCollection ParseMtrSop(string filePath)
	{
		var result = new MtrCsvDataCollection
		{
			MtrSop = NasrCsvReader.ProcessLines(
				filePath,
				fields => new MtrSop
				{
					EffDate = fields["EFF_DATE"],
					RouteTypeCode = fields["ROUTE_TYPE_CODE"],
					RouteId = fields["ROUTE_ID"],
					Artcc = fields["ARTCC"],
					SopSeqNo = NasrCsvReader.ParseInt(fields["SOP_SEQ_NO"]),
					SopText = fields["SOP_TEXT"],
				})
		};

		return result;
	}

	/// <summary>Reads <c>MTR_TERR.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="MtrCsvDataCollection.MtrTerr"/> filled in.</returns>
	public MtrCsvDataCollection ParseMtrTerr(string filePath)
	{
		var result = new MtrCsvDataCollection
		{
			MtrTerr = NasrCsvReader.ProcessLines(
				filePath,
				fields => new MtrTerr
				{
					EffDate = fields["EFF_DATE"],
					RouteTypeCode = fields["ROUTE_TYPE_CODE"],
					RouteId = fields["ROUTE_ID"],
					Artcc = fields["ARTCC"],
					TerrainSeqNo = NasrCsvReader.ParseInt(fields["TERRAIN_SEQ_NO"]),
					TerrainText = fields["TERRAIN_TEXT"],
				})
		};

		return result;
	}

	/// <summary>Reads <c>MTR_WDTH.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="MtrCsvDataCollection.MtrWdth"/> filled in.</returns>
	public MtrCsvDataCollection ParseMtrWdth(string filePath)
	{
		var result = new MtrCsvDataCollection
		{
			MtrWdth = NasrCsvReader.ProcessLines(
				filePath,
				fields => new MtrWdth
				{
					EffDate = fields["EFF_DATE"],
					RouteTypeCode = fields["ROUTE_TYPE_CODE"],
					RouteId = fields["ROUTE_ID"],
					Artcc = fields["ARTCC"],
					WidthSeqNo = NasrCsvReader.ParseInt(fields["WIDTH_SEQ_NO"]),
					WidthText = fields["WIDTH_TEXT"],
				})
		};

		return result;
	}

}

/// <summary>
/// Every parsed row of the NASR <c>MTR</c> CSV files, one list per file.
/// </summary>
public class MtrCsvDataCollection
{
	/// <summary>The rows of <c>MTR_AGY.csv</c>.</summary>
	public List<MtrAgy> MtrAgy { get; set; } = [];
	/// <summary>The rows of <c>MTR_BASE.csv</c>.</summary>
	public List<MtrBase> MtrBase { get; set; } = [];
	/// <summary>The rows of <c>MTR_PT.csv</c>.</summary>
	public List<MtrPt> MtrPt { get; set; } = [];
	/// <summary>The rows of <c>MTR_SOP.csv</c>.</summary>
	public List<MtrSop> MtrSop { get; set; } = [];
	/// <summary>The rows of <c>MTR_TERR.csv</c>.</summary>
	public List<MtrTerr> MtrTerr { get; set; } = [];
	/// <summary>The rows of <c>MTR_WDTH.csv</c>.</summary>
	public List<MtrWdth> MtrWdth { get; set; } = [];
}