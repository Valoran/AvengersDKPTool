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

        public async Task<bool> AddNewAlt(string charname, EqDkpPlayer main)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "api.php?format=json&function=character");
            var response = await _httpClient.SendAsync(req);
            if (response.IsSuccessStatusCode)
            {
                var rosterString = await response.Content.ReadAsStringAsync();

                return true;
            }
            return false;
        }

        public async Task<bool> UploadRaidLog(DateTime time,string note, ICollection<EqDkpPlayer> players)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "api.php?format=json&function=add_raid");
            var newRaid = new EqDkpNewRaidModel()
            {
                RaidDate = time.ToString("yyyy-MM-dd HH:mm"),
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

                return true;
            }
            else
            {
                var resp = await response.Content.ReadAsStringAsync();
                return false;
            }
        } 
    }
}
