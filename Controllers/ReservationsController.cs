using BasketWorld.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BasketWorld.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _ctx;
        public ReservationsController(ApplicationDbContext ctx) { _ctx = ctx; }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(int id)
        {
            var r = await _ctx.Reservations
                .Include(x => x.Lines)
                    .ThenInclude(l => l.TicketOffer)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (r == null || r.Lines == null || !r.Lines.Any())
            {
                TempData["err"] = "Réservation invalide.";
                return RedirectToAction("Index", "Home");
            }

            // re-check stock
            foreach (var l in r.Lines)
            {
                if (l.TicketOffer == null || l.TicketOffer.Available < l.Quantity)
                {
                    TempData["err"] = "Stock insuffisant.";
                    return RedirectToAction("Details", "Games", new { id = l.TicketOffer?.GameId });
                }
            }

            // décrémenter
            foreach (var l in r.Lines)
                l.TicketOffer!.Sold += l.Quantity;

            r.Status = "Paid";
            await _ctx.SaveChangesAsync();

            return RedirectToAction("Confirm", new { id = r.Id });
        }

        public async Task<IActionResult> Review(int id)
        {
            var r = await _ctx.Reservations
                .Include(x => x.Lines)
                    .ThenInclude(l => l.TicketOffer)
                        .ThenInclude(o => o.Game)
                            .ThenInclude(g => g.HomeTeam)
                .Include(x => x.Lines)
                    .ThenInclude(l => l.TicketOffer)
                        .ThenInclude(o => o.Game)
                            .ThenInclude(g => g.AwayTeam)
                .Include(x => x.Lines)
                    .ThenInclude(l => l.TicketOffer)
                        .ThenInclude(o => o.SeatCategory)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (r == null)
            {
                TempData["err"] = "Réservation introuvable.";
                return RedirectToAction("Index", "Home");
            }

            // (Conseillé) Interdire l'accès si la réservation n'appartient pas à l'utilisateur connecté
            var userId = User.Claims.First(c => c.Type.EndsWith("nameidentifier")).Value;
            if (!string.Equals(r.UserId, userId, StringComparison.Ordinal))
            {
                TempData["err"] = "Vous ne pouvez pas consulter cette réservation.";
                return RedirectToAction("Index", "Home");
            }

            if (r.Lines == null || !r.Lines.Any())
            {
                TempData["err"] = "Aucune ligne dans cette réservation.";
                return RedirectToAction("Index", "Home");
            }

            // Si une ligne est incomplète, on laisse la VIEW gérer (null-safe), mais on peut aussi bloquer ici :
            // if (r.Lines.Any(l => l.TicketOffer == null || l.TicketOffer.Game == null || l.TicketOffer.SeatCategory == null)) ...

            return View(r);
        }

        public async Task<IActionResult> Confirm(int id)
        {
            var r = await _ctx.Reservations
                .Include(x => x.Lines)
                    .ThenInclude(l => l.TicketOffer)
                        .ThenInclude(o => o.Game)
                            .ThenInclude(g => g.HomeTeam)
                .Include(x => x.Lines)
                    .ThenInclude(l => l.TicketOffer)
                        .ThenInclude(o => o.Game)
                            .ThenInclude(g => g.AwayTeam)
                .Include(x => x.Lines)
                    .ThenInclude(l => l.TicketOffer)
                        .ThenInclude(o => o.SeatCategory)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (r == null) return NotFound();

            // (Optionnel) vérifier que la résa appartient bien à l'utilisateur connecté
            var userId = User.Claims.First(c => c.Type.EndsWith("nameidentifier")).Value;
            if (!string.Equals(r.UserId, userId, StringComparison.Ordinal))
            {
                TempData["err"] = "Vous ne pouvez pas consulter cette réservation.";
                return RedirectToAction("Index", "Home");
            }

            return View(r);
        }

        [Authorize]
        public async Task<IActionResult> Mine()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var reservations = await _ctx.Reservations
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Include(r => r.Lines)
                    .ThenInclude(l => l.TicketOffer)
                        .ThenInclude(o => o.SeatCategory)
                .Include(r => r.Lines)
                    .ThenInclude(l => l.TicketOffer)
                        .ThenInclude(o => o.Game)
                            .ThenInclude(g => g.League)
                .Include(r => r.Lines)
                    .ThenInclude(l => l.TicketOffer)
                        .ThenInclude(o => o.Game)
                            .ThenInclude(g => g.HomeTeam)
                .Include(r => r.Lines)
                    .ThenInclude(l => l.TicketOffer)
                        .ThenInclude(o => o.Game)
                            .ThenInclude(g => g.AwayTeam)
                .ToListAsync();

            return View(reservations);
        }

    }
}
