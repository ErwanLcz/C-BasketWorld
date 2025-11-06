namespace BasketWorld.Models
{
    public class Reservation
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending"; // Pending|Paid|Cancelled
        public decimal TotalAmount { get; set; }
        public ICollection<ReservationLine> Lines { get; set; } = new List<ReservationLine>();
    }
}
