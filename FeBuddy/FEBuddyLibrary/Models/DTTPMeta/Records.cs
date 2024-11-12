namespace FEBuddyLibrary.Models.DTTPMeta;
public class Records
{
    public string FAAChartName { get; set; }

    public string ChartSeq { get; set; }

    public string ChartCode { get; set; }

    public string ChartName { get; set; }

    public string UserAction { get; set; }

    public string PdfName { get; set; }

    public string CnFlag { get; set; }

    public string CnSection { get; set; }

    public string CnPage { get; set; }

    public string BvSection { get; set; }

    public string BvPage { get; set; }

    public string ProcUid { get; set; }

    public string TwoColored { get; set; }

    public string Civil { get; set; }

    public string Faanfd18 { get; set; }

    public string Copter { get; set; }

    public string AmdtNum { get; set; }

    public string AmdtDate { get; set; }

    public bool HasMultiplePages { get; set; } = false;

    public int PageCount { get; set; } = 1;

    public string Variant { get; set; }

    public string AliasCommand { get; private set; }

	// Method to create an alias command based on the provided airport IATA code and chart type
	/// <summary>
	/// Creates an alias command based on the chart type and airport IATA code.
	/// </summary>
	/// <param name="AptIata">The IATA code of the airport.</param>
	public void CreateAliasComand(string AptIata)
	{
		// Initialize the alias command with the provided IATA code
		AliasCommand = $"/{AptIata}";

		// Append additional command based on chart type using a switch expression
		AliasCommand += ChartCode switch
		{
			"MIN" => HandleMinimumsChart(), // Handle "MIN" chart type (Minimums chart)
			"IAP" => HandleInstrumentApproachProcedure(AptIata), // Handle Instrument Approach Procedures
			"DP" or "ODP" => HandleDepartureProcedure(AptIata), // Handle Departure Procedures
			"HOT" => "/HS", // Hot Spot chart
			"STAR" => HandleStarProcedure(AptIata), // Handle STAR Procedures (Standard Terminal Arrival Route)
			"APD" => "/*", // Airport Diagram chart
			"LAH" => "/LAHSO", // Land and Hold Short Operations chart
			"DAU" => "/", // Data Automation Unit
			_ => "/GENERALERROR" // Default error case for unknown chart types
		};
	}

	// Handle "MIN" Chart Type (Minimums charts)
	/// <summary>
	/// Handles the alias command creation for "MIN" chart type (Minimums charts).
	/// </summary>
	/// <returns>A string representing the additional alias command for minimums chart.</returns>
	private string HandleMinimumsChart()
	{
		// Append alias command based on specific minimums chart name
		switch (ChartName)
		{
			case "TAKEOFF MINIMUMS":
				AliasCommand += "/TM";
				break;
			case "ALTERNATE MINIMUMS":
				AliasCommand += "/";
				break;
			case "DIVERSE VECTOR AREA":
				AliasCommand += "/DVA";
				break;
			case "RADAR MINIMUMS":
				AliasCommand += "/RM";
				break;
			default:
				AliasCommand += "/NEWMINTYPEERROR";
				break;
		}
		return string.Empty;
	}

	// Handle Instrument Approach Procedures (IAP)
	/// <summary>
	/// Handles the alias command creation for Instrument Approach Procedures (IAP).
	/// </summary>
	/// <param name="AptIata">The IATA code of the airport.</param>
	/// <returns>A string representing the additional alias command for IAP.</returns>
	private string HandleInstrumentApproachProcedure(string AptIata)
	{
		// Different handling based on the chart name content
		if (ChartName.IndexOf(@" OR ") != -1)
		{
			HandleOrCharts(AptIata); // Handle charts with multiple options (e.g., "A OR B")
		}
		else if (PdfName.Contains("_VIS"))
		{
			HandleVisualApproachChart(); // Handle Visual Approach Charts
		}
		else
		{
			HandleSpecificIapType(); // Handle specific types of Instrument Approach Procedures
		}
		return string.Empty;
	}

	// Handle charts with multiple options (e.g., "A OR B")
	/// <summary>
	/// Handles charts that have multiple options (e.g., "A OR B") by creating temporary records for each option.
	/// </summary>
	/// <param name="AptIata">The IATA code of the airport.</param>
	private void HandleOrCharts(string AptIata)
	{
		string runwayTempVar = ChartName.IndexOf("RWY") == -1 ? "" : ChartName.Substring(ChartName.IndexOf("RWY"));
		List<Records> tempRecordList = new List<Records>();

		// Create temporary records for each individual chart name split by " OR "
		foreach (string individualChartName in ChartName.Split(new string[] { @" OR " }, StringSplitOptions.None))
		{
			var tempRecord = CreateTempRecord(individualChartName, runwayTempVar, AptIata);
			tempRecordList.Add(tempRecord);
		}

		// Resolve variants for charts missing variant details
		ResolveVariants(tempRecordList, AptIata);
		AppendAliasCommands(tempRecordList); // Append commands to the final alias command
	}

	// Create a temporary record for handling OR charts
	/// <summary>
	/// Creates a temporary record for handling charts with multiple options.
	/// </summary>
	/// <param name="individualChartName">The name of the individual chart.</param>
	/// <param name="runwayTempVar">The runway information to append if not already present.</param>
	/// <param name="AptIata">The IATA code of the airport.</param>
	/// <returns>A DttpRecords object representing the temporary chart record.</returns>
	private Records CreateTempRecord(string individualChartName, string runwayTempVar, string AptIata)
	{
		Records tempRecord = new()
		{
			ChartCode = ChartCode,
			PdfName = PdfName,
			FAAChartName = FAAChartName,
			ChartName = individualChartName
		};

		// If the individual chart name does not include a runway, append the runway information
		if (tempRecord.ChartName.IndexOf("RWY") == -1)
		{
			tempRecord.ChartName += " " + runwayTempVar;
		}

		tempRecord.CreateAliasComand(AptIata); // Create the alias command for the temporary record
		return tempRecord;
	}

	// Resolve variants for charts missing variant details
	/// <summary>
	/// Resolves variants for charts that are missing variant details, ensuring proper alias commands are generated.
	/// </summary>
	/// <param name="tempRecordList">The list of temporary chart records.</param>
	/// <param name="AptIata">The IATA code of the airport.</param>
	private void ResolveVariants(List<Records> tempRecordList, string AptIata)
	{
		List<int> indexesMissingVariant = new();
		string tempVariant = "";
		for (int i = 0; i < tempRecordList.Count; i++)
		{
			if (string.IsNullOrEmpty(tempRecordList[i].Variant))
			{
				indexesMissingVariant.Add(i);
			}
			else
			{
				tempVariant = tempRecordList[i].Variant;
			}
		}

		// Resolve missing variants if applicable
		if (indexesMissingVariant.Count > 0 && indexesMissingVariant.Count != tempRecordList.Count)
		{
			foreach (int missingIndex in indexesMissingVariant)
			{
				if (IsVariantResolvable(tempRecordList[missingIndex]))
				{
					ResolveMissingVariant(tempRecordList[missingIndex], tempVariant);
				}
			}
		}
	}

	// Check if a chart variant can be resolved
	/// <summary>
	/// Determines if a chart variant can be resolved based on its alias command.
	/// </summary>
	/// <param name="record">The chart record to check.</param>
	/// <returns>True if the variant can be resolved; otherwise, false.</returns>
	private bool IsVariantResolvable(Records record)
	{
		return char.IsDigit(record.AliasCommand[^1]) &&
			   char.IsDigit(record.AliasCommand[^2]);
	}

	// Resolve missing variant for a given chart record
	/// <summary>
	/// Resolves the missing variant for a given chart record.
	/// </summary>
	/// <param name="record">The chart record to update.</param>
	/// <param name="tempVariant">The temporary variant to use for resolution.</param>
	private void ResolveMissingVariant(Records record, string tempVariant)
	{
		if (IsVariantResolvable(record))
		{
			string firstCommandPart = record.AliasCommand[..2];
			string endCommandPart = record.AliasCommand[^1..];
			record.AliasCommand = firstCommandPart + tempVariant + endCommandPart;
		}
		else
		{
			record.AliasCommand = record.AliasCommand.Insert(2, tempVariant);
		}
	}

	// Append alias commands from a list of chart records to the final alias command
	/// <summary>
	/// Appends alias commands from a list of chart records to the final alias command.
	/// </summary>
	/// <param name="tempRecordList">The list of temporary chart records.</param>
	private void AppendAliasCommands(List<Records> tempRecordList)
	{
		foreach (var tempRecord in tempRecordList)
		{
			AliasCommand += tempRecord.AliasCommand;
		}
	}

	// Handle Visual Approach Charts
	/// <summary>
	/// Handles the alias command creation for Visual Approach Charts.
	/// </summary>
	private void HandleVisualApproachChart()
	{
		string output = "/V";
		// Extract initials from chart name before "VISUAL" and append to the alias command
		foreach (string str in ChartName[..ChartName.IndexOf("VISUAL")].Split(' '))
		{
			if (!string.IsNullOrEmpty(str))
			{
				output += str[0];
			}
		}
		AliasCommand += output;
	}

	// Handle specific Instrument Approach Procedure types
	/// <summary>
	/// Handles the alias command creation for specific types of Instrument Approach Procedures.
	/// </summary>
	private void HandleSpecificIapType()
	{
		var chartTypeCommands = new Dictionary<string, string>
	{
		{"ILS ", "/I"}, {"LOC ", "/L"}, {"LDA ", "/D"},
		{"LDA/DME", "/A"}, {"GPS ", "/G"}, {"LOC/DME ", "/K"},
		{"NDB ", "/N"}, {"RNAV (GPS) ", "/R"}, {"SDF ", "/S"},
		{"TACAN ", "/T"}, {"VOR ", "/O"}, {"VOR/DME ", "/F"},
		{"NDB/DME ", "/B"}
	};

		// Append the appropriate alias command based on the chart type
		foreach (var chartTypeCommand in chartTypeCommands)
		{
			if (ChartName.Contains(chartTypeCommand.Key) || ChartName.Contains(chartTypeCommand.Key.Replace(" ", "-")))
			{
				var output = CreateAliasCommandHelper(chartTypeCommand.Value);
				if (!output.Contains("!DONT-INCLUDE!"))
				{
					AliasCommand += output;
				}
				return;
			}
		}

		// Handle GLS charts or unknown types
		if (ChartName.Contains("GLS "))
		{
			AliasCommand += "/";
		}
		else
		{
			AliasCommand += "/ERROR";
		}
	}

	// Handle Departure Procedure (DP/ODP)
	/// <summary>
	/// Handles the alias command creation for Departure Procedures (DP/ODP).
	/// </summary>
	/// <param name="AptIata">The IATA code of the airport.</param>
	/// <returns>A string representing the additional alias command for departure procedure.</returns>
	private string HandleDepartureProcedure(string AptIata)
	{
		// Append departure procedure alias command based on Faanfd18 data
		if (!string.IsNullOrEmpty(Faanfd18))
		{
			AliasCommand += $"/{AptIata}{Faanfd18.Split('.')[0][..^1]}";
		}
		else
		{
			AliasCommand += "/";
		}
		return string.Empty;
	}

	// Handle STAR Procedure
	/// <summary>
	/// Handles the alias command creation for STAR Procedures (Standard Terminal Arrival Route).
	/// </summary>
	/// <param name="AptIata">The IATA code of the airport.</param>
	/// <returns>A string representing the additional alias command for STAR procedure.</returns>
	private string HandleStarProcedure(string AptIata)
	{
		// Append STAR procedure alias command based on Faanfd18 data
		if (!string.IsNullOrEmpty(Faanfd18))
		{
			AliasCommand += $"/{AptIata}{Faanfd18.Split('.')[1][..^1]}";
		}
		else
		{
			AliasCommand += "/";
		}
		return string.Empty;
	}

	// Helper method to create an alias command for specific approach types
	/// <summary>
	/// Helper method to create an alias command for specific approach types, including special cases like copter or continuation charts.
	/// </summary>
	/// <param name="approachTypeCode">The approach type code to append.</param>
	/// <returns>A string representing the additional alias command for the approach type.</returns>
	private string CreateAliasCommandHelper(string approachTypeCode)
	{
		if (IsCopterOrHighChart())
		{
			return approachTypeCode + "!DONT-INCLUDE!";
		}

		if (ChartName.Contains("CONT."))
		{
			HandleContinuationChart();
		}

		return ChartName.Contains("RWY") ? HandleRunwayChart(approachTypeCode) : HandleNonRunwayChart(approachTypeCode);
	}

	// Check if the chart is a copter or HI- type
	/// <summary>
	/// Checks if the chart is a copter or HI- type.
	/// </summary>
	/// <returns>True if the chart is a copter or HI- type; otherwise, false.</returns>
	private bool IsCopterOrHighChart()
	{
		return ChartName.Contains("COPTER") || ChartName.Contains("HI-");
	}

	// Handle continuation charts that span multiple pages
	/// <summary>
	/// Handles continuation charts that span multiple pages, updating the chart name and page count accordingly.
	/// </summary>
	private void HandleContinuationChart()
	{
		HasMultiplePages = true;
		PageCount++;
		ChartName = ChartName.Replace($"{ChartName.Substring(ChartName.IndexOf(", C"))}", string.Empty);
	}

	// Handle charts that do not have runway information
	/// <summary>
	/// Handles alias command creation for charts that do not have runway information.
	/// </summary>
	/// <param name="approachTypeCode">The approach type code to append.</param>
	/// <returns>A string representing the alias command for non-runway charts.</returns>
	private string HandleNonRunwayChart(string approachTypeCode)
	{
		string output = approachTypeCode;

		if (ChartName.Contains("-"))
		{
			return AppendVariant(output, ChartName.Split('-')[1]);
		}
		else if (ChartName.Split(' ').Length >= 2)
		{
			return AppendVariant(output, ChartName.Split(' ')[1]);
		}
		else
		{
			return AppendPageCountIfNeeded(output);
		}
	}

	// Append the variant to the output string
	/// <summary>
	/// Appends the variant to the output string for non-runway charts.
	/// </summary>
	/// <param name="output">The output string to append the variant to.</param>
	/// <param name="variant">The variant to append.</param>
	/// <returns>A string representing the updated alias command with the variant appended.</returns>
	private string AppendVariant(string output, string variant)
	{
		Variant = variant;
		output += variant;
		return AppendPageCountIfNeeded(output);
	}

	// Append the page count if needed
	/// <summary>
	/// Appends the page count to the output string if the chart spans multiple pages.
	/// </summary>
	/// <param name="output">The output string to append the page count to.</param>
	/// <returns>A string representing the updated alias command with the page count appended if needed.</returns>
	private string AppendPageCountIfNeeded(string output)
	{
		if (HasMultiplePages)
		{
			output += $"{PageCount}";
		}
		return output;
	}

	// Handle charts that contain runway information
	/// <summary>
	/// Handles alias command creation for charts that contain runway information.
	/// </summary>
	/// <param name="approachTypeCode">The approach type code to append.</param>
	/// <returns>A string representing the alias command for runway charts.</returns>
	private string HandleRunwayChart(string approachTypeCode)
	{
		string output = approachTypeCode;
		bool getTwoDigitRwy = DetermineRunwayVariant(ref output);

		if (IsSingleDesignatorRunway())
		{
			return AppendRunwayDesignator(output, getTwoDigitRwy);
		}
		else
		{
			return HandleMultipleRunwayDesignators(output);
		}
	}

	// Determine if the chart has a runway variant
	/// <summary>
	/// Determines if the chart has a runway variant and updates the output string accordingly.
	/// </summary>
	/// <param name="output">The output string to append the variant to.</param>
	/// <returns>True if the chart requires a two-digit runway variant; otherwise, false.</returns>
	private bool DetermineRunwayVariant(ref string output)
	{
		if (ChartName.Contains("-"))
		{
			Variant = ChartName.Split('-')[1][0].ToString();
			output += Variant;
			return false;
		}
		else if (ChartName.Substring(0, ChartName.IndexOf("RWY")).Split(' ').Length > 2)
		{
			Variant = ChartName.Substring(0, ChartName.IndexOf("RWY")).Split(' ')[1];
			output += Variant;
			return false;
		}
		return true;
	}

	// Check if the runway has a single designator
	/// <summary>
	/// Checks if the runway has a single designator.
	/// </summary>
	/// <returns>True if the runway has a single designator; otherwise, false.</returns>
	private bool IsSingleDesignatorRunway()
	{
		return ChartName.Substring(ChartName.IndexOf("RWY")).IndexOf("/") == -1;
	}

	// Append the runway designator to the output string
	/// <summary>
	/// Appends the runway designator to the output string.
	/// </summary>
	/// <param name="output">The output string to append the runway designator to.</param>
	/// <param name="getTwoDigitRwy">Indicates whether a two-digit runway designator is required.</param>
	/// <returns>A string representing the updated alias command with the runway designator appended.</returns>
	private string AppendRunwayDesignator(string output, bool getTwoDigitRwy)
	{
		output += getTwoDigitRwy ? ChartName[^2..] : ChartName[^1..];
		return AppendPageCountIfNeeded(output);
	}

	// Handle charts with multiple runway designators
	/// <summary>
	/// Handles alias command creation for charts with multiple runway designators.
	/// </summary>
	/// <param name="output">The output string to append the runway designators to.</param>
	/// <returns>A string representing the updated alias command with multiple runway designators appended.</returns>
	private string HandleMultipleRunwayDesignators(string output)
	{
		string tempRwyNumber = ChartName.Substring(ChartName.IndexOf("RWY")).Trim().Split('/')[0].Substring(4, 2);
		int tempCount = 0;
		string tempOutput = output;

		foreach (string designator in ChartName.Substring(ChartName.IndexOf("RWY")).Split('/'))
		{
			if (tempCount > 0)
			{
				output += tempOutput + tempRwyNumber[1] + designator;
			}
			else
			{
				output += designator[^2..];
			}

			if (HasMultiplePages)
			{
				output += $"{PageCount}";
			}

			tempCount++;
		}

		return output;
	}
}
