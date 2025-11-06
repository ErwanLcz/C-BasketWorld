namespace BasketWorld.Models
{
    public class TicketOffer
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public Game Game { get; set; } = null!;
        public int SeatCategoryId { get; set; }
        public SeatCategory SeatCategory { get; set; } = null!;
        public decimal Price { get; set; }
        public int Quota { get; set; }
        public int Sold { get; set; }
        public int Available => Math.Max(0, Quota - Sold);
    }
}
