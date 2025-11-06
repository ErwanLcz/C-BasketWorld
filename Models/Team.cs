namespace BasketWorld.Models
{
    public class Team
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int LeagueId { get; set; }
        public League League { get; set; } = null!;
        public string? LogoUrl { get; set; }
        public string? HeroImageUrl { get; set; }
        public ICollection<Game> HomeGames { get; set; } = new List<Game>();
        public ICollection<Game> AwayGames { get; set; } = new List<Game>();

        public int? ExternalId { get; set; }      // id balldontlie
        public string? Abbreviation { get; set; } // LAL, BOS, etc.
    }
}
