namespace BasketWorld.Models
{
    public class SeatCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public ICollection<TicketOffer> Offers { get; set; } = new List<TicketOffer>();
    }
}
