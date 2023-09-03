using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FEBuddyLibrary.Models.Navaids;

/// <summary>
/// Represents a base class for Navaids, which are radio navigation aids used in aviation.
/// This class defines common properties and behavior for various types of Navaids.
/// </summary>
public class NavaidBase
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

  /// <summary>
  /// Gets or sets the identifier of the Navaid.
  /// </summary>
  public string Id { get; set; }

  /// <summary>
  /// Gets or sets the frequency of the Navaid.
  /// </summary>
  public string Frequency { get; set; }

  /// <summary>
  /// Gets or sets the name of the Navaid.
  /// </summary>
  public string Name { get; set; }

  /// <summary>
  /// Gets or sets the type of the Navaid.
  /// </summary>
  public string Type { get; set; }

  /// <summary>
  /// Gets or sets the location of the Navaid represented by a <see cref="Location.Location"/> object.
  /// </summary>
  public Location.Location Location { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}