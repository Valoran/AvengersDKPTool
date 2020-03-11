using AvengersDKPTool.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AvengersDKPTool.Api
{
    public class ApiMethods
    {
        public string _apiToken;
        private HttpClient _httpClient;

        public ApiMethods(string apiToken)
        {
            _apiToken = apiToken;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://eq-avengers.com/api.php?format=json&");
            _httpClient.DefaultRequestHeaders.Add("X-Custom-Authorization", "token="+_apiToken + "&type=user");
            

        }

        public void UpdateApiToken(string token)
        {
            _apiToken = token;
            _httpClient.DefaultRequestHeaders.Remove("X-Custom-Authorization");
            _httpClient.DefaultRequestHeaders.Add("X-Custom-Authorization", "token=" + _apiToken + "&type=user");
        }
        public async Task<bool> ValidateTokenAsync()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "api.php?format=json&function=me");
            var response = await _httpClient.SendAsync(req);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                var userData = JsonConvert.DeserializeObject<EqDkpUserResponse>(result);
                return userData?.Data != null;
            }
            return false;
        }

        public async Task<Dictionary<string, EqDkpPlayer>> GetRosterAsync()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "api.php?format=json&function=points");
            var response = await _httpClient.SendAsync(req);
            if (response.IsSuccessStatusCode)
            {
                var rosterString = await response.Content.ReadAsStringAsync();
                var rosterObj = JsonConvert.DeserializeObject<EqDkpGetPointsModel>(rosterString);

                return rosterObj.Players;
            }
            return new Dictionary<string, EqDkpPlayer>();
        }

        public async Task<bool> UploadRaidLog(DateTime time,string note, ICollection<EqDkpPlayer> players)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "api.php?format=json&function=add_raid");
            var updated = time.AddMinutes(30);
            var roundedTime = new DateTime(updated.Year, updated.Month, updated.Day,
                                 updated.Hour, 0, 0, time.Kind);

            var newRaid = new EqDkpNewRaidModel()
            {
                RaidDate = roundedTime.ToString("yyyy-MM-dd HH:mm"),
                RaidEventId = 1,
                RaidNote = note,
                RaidValue = 2,
                RaidAttendees = new EqDkpAttendee() { Member = players.Select(x => x.Id).ToList() }
            };
            var jsonString = JsonConvert.SerializeObject(newRaid);
            var stringContent = new StringContent(jsonString);
            stringContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            req.Content = stringContent;
            var response = await _httpClient.SendAsync(req);
            if (response.IsSuccessStatusCode)
            {
                var rosterString = await response.Content.ReadAsStringAsync();
                dynamic parsed = JsonConvert.DeserializeObject(rosterString);
                if (parsed.status == 1)
                {
                    return true;
                }
                else return false;
            }
            else
            {
                var resp = await response.Content.ReadAsStringAsync();
                return false;
            }
        } 

        public async Task<List<EqDkpRaidListModel>> GetRaidList()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "api.php?format=json&function=raids");
            var response = await _httpClient.SendAsync(req);
            if (response.IsSuccessStatusCode)
            {
                var rosterString = await response.Content.ReadAsStringAsync();
                rosterString = rosterString.Replace(",\"status\":1", "");
                try
                {

                var raidList = JsonConvert.DeserializeObject<Dictionary<string,EqDkpRaidListModel>>(rosterString);
                return raidList.Select(x=>x.Value).ToList();
                }
                catch (Exception ex)
                {

                }
            }
            return new List<EqDkpRaidListModel>();
        }

    }
}
