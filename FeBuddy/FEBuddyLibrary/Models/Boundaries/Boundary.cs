using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FEBuddyLibrary.Models.Boundaries;
public class Boundary
{
  public string Identifier { get; set; }
  public string Type { get; set; }
  public List<Waypoints.BoundaryPoints> AllPoints { get; set; }

  public Boundary(string identifier, string type, List<Waypoints.BoundaryPoints> allPoints)
  {
    Identifier = identifier;
    Type = type;
    AllPoints = allPoints;
  }
}
