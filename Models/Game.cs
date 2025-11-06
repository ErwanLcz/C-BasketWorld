namespace BasketWorld.Models
{
    public class Game
    {
        public int Id { get; set; }

        public int LeagueId { get; set; }
        public League League { get; set; } = null!;

        public int HomeTeamId { get; set; }
        public Team HomeTeam { get; set; } = null!;

        public int AwayTeamId { get; set; }
        public Team AwayTeam { get; set; } = null!;

        public DateTime StartAt { get; set; }    // date + heure
        public string Venue { get; set; } = null!; // salle/ville

        public int? ExternalId { get; set; }      // id balldontlie
        public string? Source { get; set; }       // "balldontlie"
        public int? Season { get; set; }          // 2025, etc.
        public string? Status { get; set; }       // "Final", "Scheduled", etc.
        public int? HomeScore { get; set; }       // score final ou null si pas joué
        public int? AwayScore { get; set; }       // score final ou null si pas joué


        public ICollection<TicketOffer> Offers { get; set; } = new List<TicketOffer>();
    }
}
