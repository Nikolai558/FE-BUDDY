using FEBuddyLibrary.Handlers;
using FEBuddyLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace FEBuddyLibrary.Modules.AiracData;
public class DataAccess
{
	private readonly IappSettings _settings;

	public DataAccess(IappSettings settings)
	{
		_settings = settings;
	}

	private static readonly Dictionary<string, string> _baseUrls = new()
	{
		{ "STARDP", "https://nfdc.faa.gov/webContent/28DaySub/{0}/STARDP.zip" },
		{ "APT", "https://nfdc.faa.gov/webContent/28DaySub/{0}/APT.zip" },
		{ "ARB", "https://nfdc.faa.gov/webContent/28DaySub/{0}/ARB.zip" },
		{ "ATS", "https://nfdc.faa.gov/webContent/28DaySub/{0}/ATS.zip" },
		{ "AWY", "https://nfdc.faa.gov/webContent/28DaySub/{0}/AWY.zip" },
		{ "FIX", "https://nfdc.faa.gov/webContent/28DaySub/{0}/FIX.zip" },
		{ "NAV", "https://nfdc.faa.gov/webContent/28DaySub/{0}/NAV.zip" },
		{ "AWOS", "https://nfdc.faa.gov/webContent/28DaySub/{0}/AWOS.zip" },
		{ "TELEPHONY", "https://www.faa.gov/air_traffic/publications/atpubs/cnt_html/chap3_section_2.html" },
		{ "NWS-WX-STATIONS", "https://aviationweather.gov/data/cache/stations.cache.xml.gz" },
		{ "FAA_Meta", "https://aeronav.faa.gov/d-tpp/{0}/xml_data/d-tpp_Metafile.xml" }
	};

	private static Dictionary<string, string> BuildFileUrls(string effectiveDate, string airacCycle, bool includeMeta)
	{
		var urls = new Dictionary<string, string>();
		foreach (var baseUrl in _baseUrls)
		{
			var fileName = string.Format(baseUrl.Key.Contains("FAA_Meta") || baseUrl.Key.Contains("TELEPHONY")
				? $"{airacCycle}_{baseUrl.Key}"
				: $"{effectiveDate}_{baseUrl.Key}");
			if (!includeMeta && (baseUrl.Key.Contains("Meta") || baseUrl.Key.Contains("TELEPHONY")))
				continue;
			urls[fileName] = string.Format(baseUrl.Value, effectiveDate, airacCycle);
		}
		return urls;
	}

	private static async Task DownloadFileAsync(HttpClient client, string url, string filePath)
	{
		using var response = await client.GetAsync(url);
		response.EnsureSuccessStatusCode();

		var fileBytes = await response.Content.ReadAsByteArrayAsync();
		await File.WriteAllBytesAsync(filePath, fileBytes);
	}

	private async Task<List<string>> DownloadAllFilesAsync(string effectiveDate, string airacCycle, bool getMetaFile)
	{
		var urls = BuildFileUrls(effectiveDate, airacCycle, getMetaFile);

		var downloadedFilePaths = new List<string>();
		
		using var client = new HttpClient();

		foreach (var file in urls)
		{
			var filePath = Path.Combine(FileHandler._tempDirectory, file.Key);
			if (File.Exists(filePath) && _settings.DevMode)
			{
				downloadedFilePaths.Add(filePath);
				continue;
			}

			try
			{
				await DownloadFileAsync(client, file.Value, filePath);
				downloadedFilePaths.Add(filePath);
			}
			catch (Exception ex)
			{
				HandleDownloadError(file.Key, file.Value, ex);
			}
		}
		return downloadedFilePaths;
	}

	private void HandleDownloadError(string fileName, string url, Exception ex)
	{
		// TODO - Some smart Error Message to the user or program? 
		throw ex;
	}

	public async void GetData(string effectiveDate, string airacCycle, bool getMetaFile = true)
	{
		var downloadedFiles = await DownloadAllFilesAsync(effectiveDate,airacCycle, getMetaFile);
		FileHandler.UnzipAllDownloads(downloadedFiles);
	}
}
