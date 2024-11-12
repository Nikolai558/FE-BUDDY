using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FEBuddyLibrary.Models.DTTPMeta;
public class DttpRecords
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

  public void CreateAliasComand(string AptIata)
  {
    if (ChartCode == "MIN")
    {
      HandleMinimumsChart();
    }
    else if (ChartCode == "IAP")
    {
      HandleInstrumentApproachProcedure(AptIata);
    }
    else if (ChartCode == "DP" || ChartCode == "ODP")
    {
      HandleDepartureProcedure(AptIata);
    }
    else if (ChartCode == "HOT")
    {
      AliasCommand += "/HS";
    }
    else if (ChartCode == "STAR")
    {
      HandleStarProcedure(AptIata);
    }
    else if (ChartCode == "APD")
    {
      AliasCommand += "/*";
    }
    else if (ChartCode == "LAH")
    {
      AliasCommand += "/LAHSO";
    }
    else if (ChartCode == "DAU")
    {
      AliasCommand += "/";
    }
    else
    {
      AliasCommand += "/GENERALERROR";
    }
  }

  // Handle "MIN" Chart Type
  private void HandleMinimumsChart()
  {
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
  }

  // Handle Instrument Approach Procedures (IAP)
  private void HandleInstrumentApproachProcedure(string AptIata)
  {
    if (ChartName.IndexOf(@" OR ") != -1)
    {
      HandleOrCharts(AptIata);
    }
    else if (PdfName.Contains("_VIS"))
    {
      HandleVisualApproachChart();
    }
    else
    {
      HandleSpecificIapType();
    }
  }

  private void HandleOrCharts(string AptIata)
  {
    string runwayTempVar = ChartName.IndexOf("RWY") == -1 ? "" : ChartName.Substring(ChartName.IndexOf("RWY"));
    List<DttpRecords> tempRecordList = new List<DttpRecords>();

    foreach (string individualChartName in ChartName.Split(new string[] { @" OR " }, StringSplitOptions.None))
    {
      var tempRecord = CreateTempRecord(individualChartName, runwayTempVar, AptIata);
      tempRecordList.Add(tempRecord);
    }

    ResolveVariants(tempRecordList, AptIata);
    AppendAliasCommands(tempRecordList);
  }

  private DttpRecords CreateTempRecord(string individualChartName, string runwayTempVar, string AptIata)
  {
    DttpRecords tempRecord = new()
    {
      ChartCode = ChartCode,
      PdfName = PdfName,
      FAAChartName = FAAChartName,
      ChartName = individualChartName
    };

    if (tempRecord.ChartName.IndexOf("RWY") == -1)
    {
      tempRecord.ChartName += " " + runwayTempVar;
    }

    tempRecord.CreateAliasComand(AptIata);
    return tempRecord;
  }

  private void ResolveVariants(List<DttpRecords> tempRecordList, string AptIata)
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

  private bool IsVariantResolvable(DttpRecords record)
  {
    return char.IsDigit(record.AliasCommand[^1]) &&
           char.IsDigit(record.AliasCommand[^2]);
  }

  private void ResolveMissingVariant(DttpRecords record, string tempVariant)
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

  private void AppendAliasCommands(List<DttpRecords> tempRecordList)
  {
    foreach (var tempRecord in tempRecordList)
    {
      AliasCommand += tempRecord.AliasCommand;
    }
  }

  private void HandleVisualApproachChart()
  {
    string output = "/V";
    foreach (string str in ChartName[..ChartName.IndexOf("VISUAL")].Split(' '))
    {
      if (!string.IsNullOrEmpty(str))
      {
        output += str[0];
      }
    }
    AliasCommand += output;
  }

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
  private void HandleDepartureProcedure(string AptIata)
  {
    if (!string.IsNullOrEmpty(Faanfd18))
    {
      AliasCommand += $"/{AptIata}{Faanfd18.Split('.')[0][..^1]}";
    }
    else
    {
      AliasCommand += "/";
    }
  }

  // Handle STAR Procedure
  private void HandleStarProcedure(string AptIata)
  {
    if (!string.IsNullOrEmpty(Faanfd18))
    {
      AliasCommand += $"/{AptIata}{Faanfd18.Split('.')[1][..^1]}";
    }
    else
    {
      AliasCommand += "/";
    }
  }

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
  private bool IsCopterOrHighChart()
  {
    return ChartName.Contains("COPTER") || ChartName.Contains("HI-");
  }

  // Handle continuation charts that span multiple pages
  private void HandleContinuationChart()
  {
    HasMultiplePages = true;
    PageCount++;
    ChartName = ChartName.Replace($"{ChartName.Substring(ChartName.IndexOf(", C"))}", string.Empty);
  }

  // Handle charts that do not have runway information
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
  private string AppendVariant(string output, string variant)
  {
    Variant = variant;
    output += variant;
    return AppendPageCountIfNeeded(output);
  }

  // Append the page count if needed
  private string AppendPageCountIfNeeded(string output)
  {
    if (HasMultiplePages)
    {
      output += $"{PageCount}";
    }
    return output;
  }

  // Handle charts that contain runway information
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
  private bool IsSingleDesignatorRunway()
  {
    return ChartName.Substring(ChartName.IndexOf("RWY")).IndexOf("/") == -1;
  }

  // Append the runway designator to the output string
  private string AppendRunwayDesignator(string output, bool getTwoDigitRwy)
  {
    output += getTwoDigitRwy ? ChartName[^2..] : ChartName[^1..];
    return AppendPageCountIfNeeded(output);
  }

  // Handle charts with multiple runway designators
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
