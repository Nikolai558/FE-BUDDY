using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Vatsim.Nas.Crc.Events;
using Vatsim.Nas.Crc.FlightPlans;
using Vatsim.Nas.Crc.Io;
using Vatsim.Nas.Crc.NasData;
using Vatsim.Nas.Crc.OpenPositions;
using Vatsim.Nas.Crc.Sessions;
using Vatsim.Nas.Crc.Settings;
using Vatsim.Nas.Crc.Ui.Formatting;
using Vatsim.Nas.Crc.Utils;
using Vatsim.Nas.Crc.Weather;
using Vatsim.Nas.Data.Navigation;
using Vatsim.Nas.Messaging.Entities;

namespace Vatsim.Nas.Crc.CommandProcessing;

public class AliasParser : IAliasParser, IDisposable
{
	private const string DATA_MISSING = "----";
	private const int NEAR_AIRPORT_MAX_DISTANCE = 25;

	private struct DynamicVar
	{
		public string Name { get; set; }
		public bool HasArgument { get; set; }
		public DynamicVar(string name, bool hasArg)
			: this()
		{
			Name = name;
			HasArgument = hasArg;
		}
	}

	private sealed record AliasInfo(int ArgumentCount, string[] ReplacementText);

	private bool mIsDisposed;
	private readonly Dictionary<string, AliasInfo> mAliasMap = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly List<DynamicVar> mDynamicVars = new();
	private TargetContext? mTargetContext;
	private FlightPlan? mFlightPlanContext;
	private readonly ISessionManager mSessionManager;
	private readonly IFlightPlanRepository mFlightPlanRepository;
	private readonly GeneralSettings mGeneralSettings;
	private readonly IMetarRepository mMetarRepository;
	private readonly INavDataRepository mNavDataRepository;
	private readonly IOpenPositionRepository mOpenPositionRepository;

	private ClientSession Session => mSessionManager.CurrentSession;

	public AliasParser(
		ISessionManager sessionManager,
		IFlightPlanRepository flightPlanRepository,
		GeneralSettings generalSettings,
		IMetarRepository metarRepository,
		INavDataRepository navDataRepository,
		IOpenPositionRepository openPositionRepository
	)
	{
		mSessionManager = sessionManager;
		mFlightPlanRepository = flightPlanRepository;
		mGeneralSettings = generalSettings;
		mMetarRepository = metarRepository;
		mNavDataRepository = navDataRepository;
		mOpenPositionRepository = openPositionRepository;

		mDynamicVars.Add(new DynamicVar("$squawk", false));
		mDynamicVars.Add(new DynamicVar("$arr", false));
		mDynamicVars.Add(new DynamicVar("$dep", false));
		mDynamicVars.Add(new DynamicVar("$cruise", false));
		mDynamicVars.Add(new DynamicVar("$calt", false));
		mDynamicVars.Add(new DynamicVar("$callsign", false));
		mDynamicVars.Add(new DynamicVar("$com1", false));
		mDynamicVars.Add(new DynamicVar("$myrealname", false));
		mDynamicVars.Add(new DynamicVar("$winds", false));
		mDynamicVars.Add(new DynamicVar("$myrw", false));
		mDynamicVars.Add(new DynamicVar("$mypvtrw", false));
		mDynamicVars.Add(new DynamicVar("$time", false));
		mDynamicVars.Add(new DynamicVar("$alt", false));
		mDynamicVars.Add(new DynamicVar("$temp", false));
		mDynamicVars.Add(new DynamicVar("$aircraft", false));
		mDynamicVars.Add(new DynamicVar("$route", false));
		mDynamicVars.Add(new DynamicVar("$atiscode", false));
		mDynamicVars.Add(new DynamicVar("$metar", true));
		mDynamicVars.Add(new DynamicVar("$altim", true));
		mDynamicVars.Add(new DynamicVar("$wind", true));
		mDynamicVars.Add(new DynamicVar("$radioname", true));
		mDynamicVars.Add(new DynamicVar("$freq", true));
		mDynamicVars.Add(new DynamicVar("$dist", true));
		mDynamicVars.Add(new DynamicVar("$bear", true));
		mDynamicVars.Add(new DynamicVar("$oclock", true));
		mDynamicVars.Add(new DynamicVar("$ftime", true));
		mDynamicVars.Add(new DynamicVar("$type", true));
		mDynamicVars.Add(new DynamicVar("$atccallsign", true));
		mDynamicVars.Add(new DynamicVar("$uc", true));
		mDynamicVars.Add(new DynamicVar("$lc", true));

		EventBus.Register(this);
	}

	public async void HandleEvent(ClientSessionStarted _)
	{
		await LoadAliases();
	}

	public async Task LoadAliases()
	{
		mAliasMap.Clear();

		try {
			string path = PathProvider.GetAliasesPath(Session.Artcc.Id);
			if (File.Exists(path)) {
				Log.Information("Loading aliases from {Path}", path);
				await LoadAliases(path);
			}
		}
		catch (Exception ex) {
			Log.Error(ex, "Error loading aliases");
		}

		try {
			string path = PathProvider.PersonalAliasesFilePath;
			if (File.Exists(path)) {
				Log.Information("Loading personal aliases from {Path}", path);
				await LoadAliases(path);
			}
		}
		catch (Exception ex) {
			Log.Error(ex, "Error loading personal aliases");
		}
	}

	private async Task LoadAliases(string path)
	{
		foreach (string line in await File.ReadAllLinesAsync(path)) {
			string lineTrimmed = line.Trim();

			if (!lineTrimmed.StartsWith('.')) {
				continue;
			}

			if (lineTrimmed.Length < 4) {
				continue;
			}

			if (!RegexUtils.IsMatch(line, @"^(\.\w+)\s+(.+)$", out Match match)) {
				continue;
			}

			string alias = match.Groups[1].Value;
			string replacement = match.Groups[2].Value;

			int argumentCount = 0;
			while (true) {
				string placeholder = $"${argumentCount + 1}";
				if (replacement.Contains(placeholder)) {
					argumentCount++;
				} else {
					break;
				}
			}
			mAliasMap[alias] = new AliasInfo(argumentCount, replacement.Tokenize());
		}
	}

	public string Parse(string userText, string? aircraftId = null)
	{
		List<string> tokens = userText.Tokenize().ToList();

		if (tokens.Count == 0) {
			return userText;
		}

		// First, process all aliases. Don't do more than 1000 passes to avoid infinite recursion.
		int passCount = 1000;
		for (int n = tokens.Count - 1; n >= 0; n--) {
			if (--passCount <= 0) {
				throw new ArgumentException("Infinite recursion detected. Possible self-referential alias.");
			}

			if (mAliasMap.ContainsKey(tokens[n])) {

				// Starting from the token AFTER our alias command, copy enough tokens to meet the argument requirement.
				List<string> parameters = new();
				int varsTaken = 0;
				for (int i = 0; i < mAliasMap[tokens[n]].ArgumentCount; i++) {
					if (i + n + 1 < tokens.Count) {
						parameters.Add(tokens[i + n + 1]);
						varsTaken++;
					} else {
						parameters.Add("");
					}
				}

				string[] result;

				// If there are no parameters to parse out, just copy the text. Otherwise parse the alias.
				if (parameters.Count == 0) {
					result = mAliasMap[tokens[n]].ReplacementText;
				} else {
					SubstituteParameters(mAliasMap[tokens[n]].ReplacementText, parameters, out result);
				}

				// Erase the old alias from the tokens and replace it with the parsed text.
				tokens.RemoveRange(n, varsTaken + 1);
				tokens.InsertRange(n, result);

				// Reset the loop to check from the back again.
				n = tokens.Count;
			}
		}
		string parsedResult = string.Join(" ", tokens.ToArray());
		return ParseDynamicVars(parsedResult, aircraftId);
	}

	private static void SubstituteParameters(string[] alias, List<string> userText, out string[] result)
	{
		result = alias.ToArray();
		for (int n = 0; n < userText.Count; n++) {
			string placeholder = $"${n + 1}";
			for (int i = 0; i < result.Length; i++) {
				result[i] = result[i].Replace(placeholder, userText[n]);
			}
		}
	}

	private string ParseDynamicVars(string input, string? aircraftId = null)
	{
		Match match = Regex.Match(input, @"^\s*\$context\((.+?)\)\s+(.+)$");
		if (match.Success) {
			aircraftId = match.Groups[1].Value.ToUpper();
			input = match.Groups[2].Value;
		}

		mTargetContext = null;
		mFlightPlanContext = null;

		if (!string.IsNullOrEmpty(aircraftId)) {
			TargetContextRequested request = new(aircraftId);
			EventBus.Publish(this, request);
			mTargetContext = request.Context;
			if (mTargetContext is not null) {
				mFlightPlanContext = mFlightPlanRepository.Get(mTargetContext.AircraftId);
			}
		}

		foreach (string name in mDynamicVars.Where(v => !v.HasArgument).Select(v => v.Name)) {
			string pattern = $@"(\{name})(\W)";
			if (Regex.IsMatch(input, pattern)) {
				input = Regex.Replace(input, pattern, new MatchEvaluator(DynamicVarMatchEvaluator));
			}
			pattern = $@"\{name}$";
			if (Regex.IsMatch(input, pattern)) {
				input = Regex.Replace(input, pattern, HandleDynamicVar(name, ""));
			}
		}

		foreach (DynamicVar dynamicVar in mDynamicVars.Where(v => v.HasArgument)) {
			string pattern = $@"(\{dynamicVar.Name})\((.*?)\)";
			if (Regex.IsMatch(input, pattern)) {
				input = Regex.Replace(input, pattern, new MatchEvaluator(DynamicFunctionMatchEvaluator));
			}
		}
		return input;
	}

	private string DynamicVarMatchEvaluator(Match match)
	{
		return HandleDynamicVar(match.Groups[1].Value, "") + match.Groups[2].Value;
	}

	private string DynamicFunctionMatchEvaluator(Match match)
	{
		return HandleDynamicVar(match.Groups[1].Value, match.Groups[2].Value);
	}

	private string HandleDynamicVar(string varName, string argument)
	{
		try {
			switch (varName) {
				// Variables:
				case "$squawk":
					return mFlightPlanContext?.AssignedBeaconCode?.ToString("0000") ?? DATA_MISSING;
				case "$arr":
					return (mFlightPlanContext is not null && !string.IsNullOrWhiteSpace(mFlightPlanContext.Destination)) ? mFlightPlanContext.Destination : DATA_MISSING;
				case "$dep":
					return (mFlightPlanContext is not null && !string.IsNullOrWhiteSpace(mFlightPlanContext.Departure)) ? mFlightPlanContext.Departure : DATA_MISSING;
				case "$cruise":
					return (mFlightPlanContext is not null) ? FormatFlightPlanAltitude(mFlightPlanContext.Altitude) : DATA_MISSING;
				case "$calt": {
					if ((mTargetContext is null) || !mTargetContext.Altitude.HasValue) {
						return DATA_MISSING;
					}
					int altitude = mTargetContext.Altitude.Value;
					return FormatAltitude(altitude);
				}
				case "$callsign":
					return Session.Callsign ?? DATA_MISSING;
				case "$com1":
					return FrequencyFormatter.Format(Session.PrimaryPosition.Frequency);
				case "$myrealname":
					return mGeneralSettings.RealName;
				case "$winds": {
					if (mTargetContext is null) {
						return "unknown";
					}
					if (!string.IsNullOrWhiteSpace(mTargetContext.AirportId)) {
						if (GetMetar(mTargetContext.AirportId) is Metar metar) {
							return GetWindString(metar);
						}
						return "unknown";
					}
					if (mFlightPlanContext is null) {
						return "unknown";
					}
					if ((GetCloserAirportId(mFlightPlanContext, mTargetContext.Location) is string stationId) && (GetMetar(stationId) is Metar metar2)) {
						return GetWindString(metar2);
					}
					return "unknown";
				}
				case "$myrw":
					return "";
				case "$mypvtrw":
					return "$";
				case "$time":
					return DateTime.UtcNow.ToString("HH:mm");
				case "$alt":
					if ((mTargetContext is not null) && mTargetContext.TemporaryAltitude.HasValue && (mTargetContext.TemporaryAltitude.Value < 100000)) {
						return FormatAltitude(mTargetContext.TemporaryAltitude.Value);
					} else if (mFlightPlanContext is not null) {
						return FormatFlightPlanAltitude(mFlightPlanContext.Altitude);
					} else {
						return DATA_MISSING;
					}
				case "$temp":
					if ((mTargetContext is not null) && mTargetContext.TemporaryAltitude.HasValue && (mTargetContext.TemporaryAltitude.Value < 100000)) {
						return FormatAltitude(mTargetContext.TemporaryAltitude.Value);
					} else {
						return DATA_MISSING;
					}
				case "$aircraft":
					return Session.SelectedAircraftId ?? DATA_MISSING;
				case "$route":
					return Regex.Replace(mFlightPlanContext?.Route ?? DATA_MISSING, @"^\+", "");
				case "$atiscode":
					return DATA_MISSING;

				// Functions:
				case "$metar":
					return GetMetar(argument.ToUpper())?.RawMetar ?? DATA_MISSING;
				case "$altim":
					return GetMetar(argument.ToUpper())?.Altimeter ?? DATA_MISSING;
				case "$wind": {
					if (GetMetar(argument.ToUpper()) is Metar metar) {
						return GetWindString(metar);
					}
					return "unknown";
				}
				case "$radioname":
					if (string.IsNullOrEmpty(argument)) {
						return Session.IsServerSessionStarted ? Session.PrimaryPosition.RadioName : DATA_MISSING;
					} else if (mOpenPositionRepository.FindOneByHandoffString(argument.ToUpper()) is OpenPositionDto position) {
						return position.RadioName ?? position.Callsign;
					} else {
						return DATA_MISSING;
					}
				case "$freq": {
					if (string.IsNullOrEmpty(argument)) {
						return Session.IsServerSessionActive ? FrequencyFormatter.Format(Session.PrimaryPosition.Frequency) : DATA_MISSING;
					} else {
						if (mOpenPositionRepository.FindOneByHandoffString(argument.ToUpper()) is OpenPositionDto openPosition) {
							return openPosition.Frequency is null ? DATA_MISSING : FrequencyFormatter.Format(openPosition.Frequency.Value);
						}
						return DATA_MISSING;
					}
				}
				case "$dist":
					if (!string.IsNullOrEmpty(argument) && (mTargetContext is not null)) {
						GeoPoint? loc = mNavDataRepository.LocateAirportOrClosestMatchingFix(argument.ToUpper(), mTargetContext.Location);
						if (loc is not null) {
							double dist = loc.GreatCircleDistanceTo(mTargetContext.Location);
							return Convert.ToInt32(dist).ToString();
						}
					}
					return DATA_MISSING;
				case "$bear":
					if (!string.IsNullOrEmpty(argument) && (mTargetContext is not null)) {
						GeoPoint? loc = mNavDataRepository.LocateAirportOrClosestMatchingFix(argument.ToUpper(), mTargetContext.Location);
						if (loc is not null) {
							double bearing = loc.BearingTo(mTargetContext.Location, GeoCalc.LongitudeScalingFactor(mTargetContext.Location));
							return bearing switch {
								>= 337.5 or < 22.5 => "north",
								>= 22.5 and < 67.5 => "northeast",
								>= 67.5 and < 112.5 => "east",
								>= 112.5 and < 157.5 => "southeast",
								>= 157.5 and < 202.5 => "south",
								>= 202.5 and < 247.5 => "southwest",
								>= 247.5 and < 292.5 => "west",
								_ => "northwest"
							};
						}
					}
					return DATA_MISSING;
				case "$oclock":
					if (!string.IsNullOrEmpty(argument) && (mTargetContext is not null) && mTargetContext.TrueGroundTrack.HasValue) {
						GeoPoint? loc = mNavDataRepository.LocateAirportOrClosestMatchingFix(argument.ToUpper(), mTargetContext.Location);
						if (loc is not null) {
							double bearing = mTargetContext.Location.BearingTo(loc, GeoCalc.LongitudeScalingFactor(mTargetContext.Location));
							bearing -= mTargetContext.TrueGroundTrack.Value;
							if (bearing < 0.0) {
								bearing += 360.0;
							}
							return bearing switch {
								>= 345.0 or < 15.0 => "twelve o'clock",
								>= 15.0 and < 45.0 => "one o'clock",
								>= 45.0 and < 75.0 => "two o'clock",
								>= 75.0 and < 105.0 => "three o'clock",
								>= 105.0 and < 135.0 => "four o'clock",
								>= 135.0 and < 165.0 => "five o'clock",
								>= 165.0 and < 195.0 => "six o'clock",
								>= 195.0 and < 225.0 => "seven o'clock",
								>= 225.0 and < 255.0 => "eight o'clock",
								>= 255.0 and < 285.0 => "nine o'clock",
								>= 285.0 and < 315.0 => "ten o'clock",
								_ => "eleven o'clock"
							};
						}
					}
					return DATA_MISSING;
				case "$ftime": {
					int minutes = 0;
					if (!string.IsNullOrEmpty(argument)) {
						minutes = int.Parse(argument);
					}
					return DateTime.UtcNow.AddMinutes(minutes).ToString("HH:mm");
				}
				case "$uc":
					return argument.ToUpper();
				case "$lc":
					return argument.ToLower();
				case "$type":
					return mFlightPlanRepository.Get(argument.ToUpper())?.AircraftType ?? DATA_MISSING;
				case "$atccallsign":
					return mOpenPositionRepository.FindOneByHandoffString(argument.ToUpper())?.Callsign ?? DATA_MISSING;
				default:
					return DATA_MISSING;
			}
		}
		catch {
			return DATA_MISSING;
		}
	}

	private string? GetCloserAirportId(FlightPlan flightPlan, GeoPoint aircraftLocation)
	{
		Airport? departureAirport = null;
		if (!string.IsNullOrWhiteSpace(flightPlan.Departure)) {
			departureAirport = mNavDataRepository.GetAirport(flightPlan.Departure);
		}
		Airport? destinationAirport = null;
		if (!string.IsNullOrWhiteSpace(flightPlan.Destination)) {
			destinationAirport = mNavDataRepository.GetAirport(flightPlan.Destination);
		}
		double? distanceToDepartureAirport = departureAirport?.Location.GreatCircleDistanceTo(aircraftLocation);
		double? distanceToDestinationAirport = destinationAirport?.Location.GreatCircleDistanceTo(aircraftLocation);
		string? stationId = null;
		if ((distanceToDepartureAirport <= NEAR_AIRPORT_MAX_DISTANCE) && (distanceToDestinationAirport <= NEAR_AIRPORT_MAX_DISTANCE)) {
			if (distanceToDepartureAirport < distanceToDestinationAirport) {
				stationId = departureAirport!.IcaoId ?? departureAirport.FaaId;
			} else {
				stationId = destinationAirport!.IcaoId ?? destinationAirport.FaaId;
			}
		} else if (distanceToDepartureAirport <= NEAR_AIRPORT_MAX_DISTANCE) {
			stationId = departureAirport!.IcaoId ?? departureAirport.FaaId;
		} else if (distanceToDestinationAirport <= NEAR_AIRPORT_MAX_DISTANCE) {
			stationId = destinationAirport!.IcaoId ?? destinationAirport.FaaId;
		}
		return stationId;
	}

	private static string GetWindString(Metar metar)
	{
		if (metar.WindDirectionVariability.HasValue) {
			if (metar.WindGusts > 0) {
				return $"variable {metar.WindDirection:000}-{metar.WindDirectionVariability:000} at {metar.WindSpeed} gusts {metar.WindGusts}";
			} else {
				return $"variable {metar.WindDirection:000}-{metar.WindDirectionVariability:000} at {metar.WindSpeed}";
			}
		} else if (metar.WindDirection is null) {
			return $"variable at {metar.WindSpeed}";
		} else if (metar.WindSpeed < 3) {
			return "calm";
		} else if (metar.WindGusts > 0) {
			return $"{metar.WindDirection:000} at {metar.WindSpeed} gusts {metar.WindGusts}";
		} else {
			return $"{metar.WindDirection:000} at {metar.WindSpeed}";
		}
	}

	private static string FormatFlightPlanAltitude(string altitude)
	{
		if (int.TryParse(altitude, out int altNumeric)) {
			if (altitude.Length <= 3) {
				altNumeric *= 100;
			}
			return FormatAltitude(altNumeric);
		} else {
			return string.IsNullOrWhiteSpace(altitude) ? DATA_MISSING : altitude;
		}
	}

	private static string FormatAltitude(int altitude)
	{
		int hundreds = Convert.ToInt32(Math.Round(altitude / 100d, 0, MidpointRounding.AwayFromZero));
		if (altitude < Constants.TRANSITION_ALTITUDE) {
			return (hundreds * 100).ToString();
		} else {
			return $"FL{hundreds}";
		}
	}

	private Metar? GetMetar(string airportId)
	{
		Airport? airport = mNavDataRepository.GetAirport(airportId);
		if ((airport is null) || string.IsNullOrWhiteSpace(airport.IcaoId)) {
			return null;
		}

		return mMetarRepository.GetMetar(airport.IcaoId);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (mIsDisposed) {
			return;
		}

		if (disposing) {
			EventBus.Unregister(this);
		}

		mIsDisposed = true;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
