using System.ComponentModel.DataAnnotations;

namespace BasketWorld.ViewModels.Admin
{
    public class GameCreateVM
    {
        [Required] public int LeagueId { get; set; }
        [Required] public int HomeTeamId { get; set; }
        [Required] public int AwayTeamId { get; set; }
        [Required] public DateTime StartAt { get; set; } = DateTime.UtcNow.AddDays(1);
        [Required] public string Venue { get; set; } = null!;

        // création des offres en même temps
        public List<OfferItem> Offers { get; set; } = new()
        {
            new OfferItem(), new OfferItem()
        };

        public class OfferItem
        {
            [Required] public int SeatCategoryId { get; set; }
            [Range(1, 100000)] public int Quota { get; set; } = 100;
            [Range(1, 100000)] public decimal Price { get; set; } = 50m;
        }
    }
}
