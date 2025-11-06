namespace BasketWorld.Models
{
    public class League
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public ICollection<Team> Teams { get; set; } = new List<Team>();
        public ICollection<Game> Games { get; set; } = new List<Game>();
    }
}
