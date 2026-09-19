using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeBuddy.Core.Models.DTTPMeta;

/// <summary>
/// Represents an airport with various properties and a list of associated records
/// </summary>
public class Airports
{
	// The name of the airport
	public string AirportName { get; set; }

	// Indicates whether the airport is a military airport
	public string Military { get; set; }

	// The identifier code of the airport (e.g., FAA identifier)
	public string AptIdent { get; set; }

	// The ICAO code of the airport (e.g., four-character international code)
	public string Icao { get; set; }

	// An alphanumeric identifier associated with the airport
	public string Alnum { get; set; }

	// A list of records associated with the airport
	public List<Records> Records { get; set; } = new List<Records>();
}
