using FEBuddyLibrary.Interfaces;
using FEBuddyLibrary.Models.DTTPMeta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FEBuddyLibrary.Modules.AiracData;
public class AiracData
{
	public event Action<int, string> AiracProgress;

	public string EffectiveDate { get; set; }
	public string AiracCycle { get; set; }

	private readonly IappSettings _settings;

	private DataAccess _dataAccess;

	public AiracData(string EffectiveDate, IappSettings settings)
	{
		this.EffectiveDate = EffectiveDate;
		this.AiracCycle = GetAiracCycle(EffectiveDate);
		this._settings = settings;
		this._dataAccess = new DataAccess(settings);
	}

	private static string GetAiracCycle(string effectiveDate)
	{
		return AiracDateCycle.AllCycleDates[effectiveDate];
	}

	public void ParseAiracData(bool includeMetaData)
	{
		// https://chatgpt.com/share/677f24f2-87b0-800c-9177-18bd0f8e2ccb
		var functions = new Action[]
		{

		};




		_dataAccess.GetData(EffectiveDate, AiracCycle, includeMetaData);








	}
	

	
}
