using BasketWorld.Data;
using BasketWorld.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BasketWorld.Controllers
{
    public class GamesController : Controller
    {
        private readonly ApplicationDbContext _ctx;
        public GamesController(ApplicationDbContext ctx) { _ctx = ctx; }

        public async Task<IActionResult> Details(int id)
        {
            var game = await _ctx.Games
                .Include(g => g.League)
                .Include(g => g.HomeTeam)
                .Include(g => g.AwayTeam)
                .Include(g => g.Offers).ThenInclude(o => o.SeatCategory)
                .FirstOrDefaultAsync(g => g.Id == id);

            return game == null ? NotFound() : View(game);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserve(int offerId, int quantity)
        {
            var offer = await _ctx.TicketOffers
                .Include(o => o.Game)
                .FirstOrDefaultAsync(o => o.Id == offerId);

            if (offer == null || quantity <= 0)
                return BadRequest("Offre invalide.");

            if (offer.Available < quantity)
            {
                TempData["err"] = "Plus assez de places disponibles.";
                return RedirectToAction("Details", new { id = offer.GameId });
            }

            var userId = User.Claims.First(c => c.Type.EndsWith("nameidentifier")).Value;

            var reservation = new Reservation
            {
                UserId = userId,
                Status = "Pending",
                TotalAmount = offer.Price * quantity,
                Lines = new List<ReservationLine>
                {
                    new ReservationLine
                    {
                        TicketOfferId = offer.Id,
                        Quantity = quantity,
                        UnitPrice = offer.Price
                    }
                }
            };

            _ctx.Reservations.Add(reservation);
            await _ctx.SaveChangesAsync();

            return RedirectToAction("Review", "Reservations", new { id = reservation.Id });
        }

    }
}
