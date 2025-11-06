namespace BasketWorld.Services
{
    public class BlGame
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }         // ISO 8601 UTC
        public BlTeam Home_team { get; set; } = new();
        public int Home_team_score { get; set; }
        public BlTeam Visitor_team { get; set; } = new();
        public int Visitor_team_score { get; set; }
        public int Season { get; set; }
        public string Status { get; set; } = "";   // "Final" | "Scheduled" | ...

        // ⚠️ types corrects
        public int? Period { get; set; }           // ex: 4
        public bool? Postseason { get; set; }      // ex: false
    }

    public class BlTeam
    {
        public int Id { get; set; }
        public string Abbreviation { get; set; } = "";
        public string City { get; set; } = "";
        public string Conference { get; set; } = "";
        public string Division { get; set; } = "";
        public string Full_name { get; set; } = "";
        public string Name { get; set; } = "";
    }

    public class BlPaginated<T>
    {
        public List<T> Data { get; set; } = new();
        public BlMeta Meta { get; set; } = new();
    }

    public class BlMeta
    {
        // balldontlie renvoie total_pages/current_page/next_page/per_page/total_count
        public int Total_pages { get; set; }
        public int Current_page { get; set; }
        public int Next_page { get; set; }
        public int Per_page { get; set; }
        public int? Total_count { get; set; }
    }
}
