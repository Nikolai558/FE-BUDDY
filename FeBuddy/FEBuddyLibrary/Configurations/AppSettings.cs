using FEBuddyLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FEBuddyLibrary.Configurations;



public class AppSettings : IappSettings
{
	public bool DevMode { get; set; }
	public string OutputDirectory { get; set; }

}
