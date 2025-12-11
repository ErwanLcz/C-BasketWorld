namespace BasketWorld.Services
{
    public class TsTeamList
    {
        public List<TsTeam>? Teams { get; set; }
    }

    public class TsTeam
    {
        public string IdTeam { get; set; } = "";
        public string StrTeam { get; set; } = "";
        public string? StrTeamShort { get; set; }
        public string? StrBadge { get; set; }     // logo
        public string? StrLogo { get; set; }      // alternative
        public string? StrFanart1 { get; set; }   // image "hero"
    }

    public class TsEventsList
    {
        // respecter le nom JSON : "events"
        public List<TsEvent>? events { get; set; }
    }


    public class TsEvent
    {
        public string IdEvent { get; set; } = "";
        public string? StrSeason { get; set; }

        // "dateEvent" dans le JSON
        public string? DateEvent { get; set; }

        // "strTime" dans le JSON
        public string? StrTime { get; set; }

        // "strTimestamp" (parfois null)
        public string? StrTimestamp { get; set; }

        public string? StrHomeTeam { get; set; }
        public string? StrAwayTeam { get; set; }

        // "idHomeTeam", "idAwayTeam"
        public string? IdHomeTeam { get; set; }
        public string? IdAwayTeam { get; set; }

        public string? StrVenue { get; set; }
        public string? StrStatus { get; set; }

        public string? IntHomeScore { get; set; }
        public string? IntAwayScore { get; set; }
    }
}
