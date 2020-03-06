using AvengersDKPTool.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AvengersDKPTool.Models
{
    public class EqDkpGetPointsModel
    {
        public EqDkp EqDkp { get; set; }
        public EqDkpGame Game { get; set; }
        public EqDkpInfo Info { get; set; }
        public Dictionary<string, EqDkpPlayer> Players { get; set; }
        [JsonConverter(typeof(BoolConverter))]
        public bool Status { get; set; }
    }

    public class EqDkp
    {
        public string Name { get; set; }
        public string Guild { get; set; }
        [JsonProperty(PropertyName = "dkp_name")]
        public string DkpName { get; set; }
        public string Version { get; set; }
        [JsonProperty(PropertyName = "base_layout")]
        public string BaseLayout { get; set; }
    }
    public class EqDkpGame
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Language { get; set; }
    }

    public class EqDkpInfo
    {
        [JsonProperty(PropertyName = "with_twink")]
        public bool WithTwink { get; set; }
        public DateTime Date { get; set; }
        public string Timestamp { get; set; }
        [JsonProperty(PropertyName = "total_players")]
        public int TotalPlayers { get; set; }
        [JsonProperty(PropertyName = "total_items")]
        public int TotalItems { get; set; }
    }

    public class EqDkpPlayer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [JsonConverter(typeof(BoolConverter))]
        public bool Active { get; set; }
        [JsonConverter(typeof(BoolConverter))]
        public bool Hidden { get; set; }
        [JsonProperty(PropertyName = "main_id")]
        public int MainId { get; set; }
        [JsonProperty(PropertyName = "main_name")]
        public string MainName { get; set; }
        [JsonProperty(PropertyName = "class_id")]
        public int ClassId { get; set; }
        [JsonProperty(PropertyName = "class_name")]
        public string ClassName { get; set; }
        public Dictionary<string, EqDkpPoints> Points { get; set; }
        public List<object> Items { get; set; }
        public List<object> Adjustments { get; set; }
    }
    public class EqDkpPoints
    {
        [JsonProperty(PropertyName = "multidkp_id")]
        public int MultidkpId { get; set; }
        [JsonProperty(PropertyName = "points_current")]
        public double Current { get; set; }
        [JsonProperty(PropertyName = "points_earned")]
        public double Earned { get; set; }
        [JsonProperty(PropertyName = "points_spent")]
        public double Spent { get; set; }
        [JsonProperty(PropertyName = "points_adjustment")]
        public double Adjustment { get; set; }
    }
    public class EqDkpUserResponse
    {
        public EqDkpUser Data { get; set; }
    }
    public class EqDkpUser
    {
        [JsonProperty(PropertyName = "user_id")]
        public int UserId { get; set; }
        [JsonProperty(PropertyName = "user_name")]
        public string UserName { get; set; }
        public List<int> UserGroups { get; set; }
    }

    public class EqDkpNewRaidModel
    {
        [JsonProperty(PropertyName = "raid_date")]
        public string RaidDate { get; set; }
        [JsonProperty(PropertyName = "raid_attendees")]
        public EqDkpAttendee RaidAttendees { get; set; }
        [JsonProperty(PropertyName = "raid_value")]
        public double RaidValue { get; set; }
        [JsonProperty(PropertyName = "raid_event_id")]
        public int RaidEventId { get; set; }
        [JsonProperty(PropertyName = "raid_note")]
        public string RaidNote { get; set; }
    }
    public class EqDkpAttendee
    {
        [JsonProperty(PropertyName = "member")]
        public List<int> Member { get; set; }
    }
}
