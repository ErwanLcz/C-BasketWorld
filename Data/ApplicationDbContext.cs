using BasketWorld.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BasketWorld.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}

        public DbSet<League> Leagues => Set<League>();
        public DbSet<Team> Teams => Set<Team>();
        public DbSet<Game> Games => Set<Game>();
        public DbSet<SeatCategory> SeatCategories => Set<SeatCategory>();
        public DbSet<TicketOffer> TicketOffers => Set<TicketOffer>();
        public DbSet<Reservation> Reservations => Set<Reservation>();
        public DbSet<ReservationLine> ReservationLines => Set<ReservationLine>();
        public DbSet<GameAccess> GameAccesses => Set<GameAccess>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<League>().HasIndex(x => x.Name).IsUnique();

            b.Entity<Game>()
                .HasOne(g => g.HomeTeam).WithMany(t => t.HomeGames)
                .HasForeignKey(g => g.HomeTeamId).OnDelete(DeleteBehavior.Restrict);

            b.Entity<Game>()
                .HasOne(g => g.AwayTeam).WithMany(t => t.AwayGames)
                .HasForeignKey(g => g.AwayTeamId).OnDelete(DeleteBehavior.Restrict);

            b.Entity<TicketOffer>()
                .HasIndex(o => new { o.GameId, o.SeatCategoryId })
                .IsUnique();
            
            b.Entity<GameAccess>()
                .HasIndex(x => new { x.UserId, x.GameId })
                .IsUnique();
        }
    }
}
