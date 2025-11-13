namespace BasketWorld.Models
{
    public class GameAccess
    {
        public int Id { get; set; }

        public int GameId { get; set; }
        public Game Game { get; set; } = null!;

        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
    }
}
