using System.Xml.Serialization;

namespace BasketWorld.Services
{
    // Racine: <schedule> ... </schedule>
    [XmlRoot("schedule")]
    public class ScheduleResponse
    {
        [XmlElement("item")]
        public List<ScheduleItem> Items { get; set; } = new();
    }

    public class ScheduleItem
    {
        [XmlElement("gameday")]
        public int GameDay { get; set; }

        [XmlElement("round")]
        public string? Round { get; set; }   // ex: "RS"

        [XmlElement("arenacode")]
        public string? ArenaCode { get; set; }

        [XmlElement("arenaname")]
        public string? ArenaName { get; set; }

        [XmlElement("arenacapacity")]
        public int ArenaCapacity { get; set; }

        // ex: "Sep 30, 2025"
        [XmlElement("date")]
        public string? DateRaw { get; set; }

        // ex: "20:45"
        [XmlElement("startime")]
        public string? StartTimeRaw { get; set; }

        [XmlElement("endtime")]
        public string? EndTimeRaw { get; set; }

        [XmlElement("group")]
        public string? Group { get; set; }   // "Regular Season"

        [XmlElement("game")]
        public int GameNumber { get; set; }  // 7

        [XmlElement("gamecode")]
        public string? GameCode { get; set; } // "E2025_7"

        [XmlElement("hometeam")]
        public string? HomeTeamName { get; set; }

        [XmlElement("homecode")]
        public string? HomeTeamCode { get; set; } // "VIR"

        [XmlElement("hometv")]
        public string? HomeTv { get; set; }

        [XmlElement("awayteam")]
        public string? AwayTeamName { get; set; }

        [XmlElement("awaycode")]
        public string? AwayTeamCode { get; set; } // "MAD"

        [XmlElement("awaytv")]
        public string? AwayTv { get; set; }

        [XmlElement("confirmeddate")]
        public bool ConfirmedDate { get; set; }

        [XmlElement("confirmedtime")]
        public bool ConfirmedTime { get; set; }

        [XmlElement("played")]
        public bool Played { get; set; }
    }

    // Le root du XML : <game ...>
    [XmlRoot("game")]
    public class EuroleagueGameDetails
    {
        [XmlAttribute("seasoncode")]
        public string? SeasonCode { get; set; }

        [XmlAttribute("code")]
        public int Code { get; set; }   // gameCode = 7

        [XmlAttribute("played")]
        public bool Played { get; set; }

        [XmlElement("localclub")]
        public EuroleagueGameClub? LocalClub { get; set; }

        [XmlElement("roadclub")]
        public EuroleagueGameClub? RoadClub { get; set; }
    }

    // <localclub ... score="74"> et <roadclub ... score="68">
    public class EuroleagueGameClub
    {
        [XmlAttribute("code")]
        public string? Code { get; set; }   // ex: "VIR"

        [XmlAttribute("name")]
        public string? Name { get; set; }   // ex: "Virtus Bologna"

        [XmlAttribute("score")]
        public int Score { get; set; }      // ex: 74
    }

    // DTO "simplifié" qu’on renverra au service de sync
    public class GameResultDto
    {
        public string? SeasonCode { get; set; }
        public int GameCode { get; set; }

        public string? HomeTeamCode { get; set; } // VIR
        public string? AwayTeamCode { get; set; } // MAD

        public int HomeScore { get; set; }
        public int AwayScore { get; set; }

        public string? Status { get; set; } // "Final" / "Scheduled"
    }
}
