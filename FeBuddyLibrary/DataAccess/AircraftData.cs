using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FeBuddyLibrary.Models;
using Newtonsoft.Json;

namespace FeBuddyLibrary.DataAccess
{
    public class AircraftData
    {
        private AircraftDataRootObject AllACData = new();

        public async void CreateAircraftDataAlias()
        {
            AircraftDataRootObject aircraftData = await GetACDataAsync();
            WriteACData(aircraftData);
        }

        private async Task<AircraftDataRootObject> GetACDataAsync()
        {
            HttpClient client = new HttpClient();

            var url = "https://www4.icao.int/doc8643/External/AircraftTypes";
            var values = new Dictionary<string, string>
            {
                {"Connection", "keep-alive" },
                {"Content-Length", "0" },
                {"Pragma", "no-cache" },
                {"Cache-Control", "no-cache" },
                {"Content-Type", "application/json" }
            };

            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync(url, content);

            string responseStringJson = await response.Content.ReadAsStringAsync();

            responseStringJson = "{\"AllAircraftData\":" + responseStringJson + "}";

            AircraftDataRootObject output = JsonConvert.DeserializeObject<AircraftDataRootObject>(responseStringJson);

            return output;
        }

        private void WriteACData(AircraftDataRootObject aircraftData)
        {

        }
    }
}
