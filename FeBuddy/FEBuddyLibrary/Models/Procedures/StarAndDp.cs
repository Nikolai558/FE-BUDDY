using FEBuddyLibrary.Models.Location;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FEBuddyLibrary.Models.Procedures;

/// <summary>
/// Represents a STAR (Standard Terminal Arrival Route) or Departure Procedure (DP) with various properties
/// </summary>
public class StarAndDp
{
	/// <summary>
	/// The type of the procedure, either STAR or DP.
	/// </summary>
	public string Type { get; set; }

	/// <summary>
	/// The sequence number for the point in the procedure.
	/// </summary>
	public string SeqNumber { get; set; }

	/// <summary>
	/// The point code associated with the procedure.
	/// </summary>
	public string PointCode { get; set; }

	/// <summary>
	/// The identifier for the point in the procedure.
	/// </summary>
	public string PointId { get; set; }

	/// <summary>
	/// The computer code used for automation purposes.
	/// </summary>
	public string ComputerCode { get; set; }

	/// <summary>
	/// The geographic location of the point in the procedure.
	/// </summary>
	public Location.Location Location { get; set; }

	/// <summary>
	/// A list of airports that this point serves.
	/// </summary>
	public List<string> AirpotsThisPointServes { get; set; } = new List<string>();
}

