namespace BasketWorld.Models
{
    public class ReservationLine
    {
        public int Id { get; set; }
        public int ReservationId { get; set; }
        public Reservation Reservation { get; set; } = null!;
        public int TicketOfferId { get; set; }
        public TicketOffer TicketOffer { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
    }
}
