using Microsoft.AspNetCore.Identity;
using BasketWorld.Models;
using Microsoft.EntityFrameworkCore;

namespace BasketWorld.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider sp)
        {
            using var scope = sp.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await ctx.Database.MigrateAsync();

            // --- PATCH: renseigne LogoUrl si manquant pour les équipes déjà existantes ---
            {
                var logoMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    // NBA (chemins alignés avec tes fichiers dans wwwroot/images/logos)
                    ["Hawks"] = "/images/logos/atlanta.png",
                    ["Celtics"] = "/images/logos/celtix.png",
                    ["Nets"] = "/images/logos/nets.png",
                    ["Hornets"] = "/images/logos/hornets.png",
                    ["Bulls"] = "/images/logos/chicago.png",
                    ["Cavaliers"] = "/images/logos/clevland.png",
                    ["Mavericks"] = "/images/logos/Mavericks.png",
                    ["Nuggets"] = "/images/logos/Nuggets.png",
                    ["Pistons"] = "/images/logos/detroit_piston.png",
                    ["Warriors"] = "/images/logos/warriors.png",
                    ["Rockets"] = "/images/logos/rokets.png",
                    ["Pacers"] = "/images/logos/Pacers.png",
                    ["Clippers"] = "/images/logos/Clippers.png",
                    ["Lakers"] = "/images/logos/Lakers.png",
                    ["Grizzlies"] = "/images/logos/grizzlies.png",
                    ["Heat"] = "/images/logos/miami.png",
                    ["Bucks"] = "/images/logos/Buks.png", // d'après ta capture
                    ["Timberwolves"] = "/images/logos/timberWolves.png",
                    ["Pelicans"] = "/images/logos/neworleans.png",
                    ["Knicks"] = "/images/logos/newyork.png",
                    ["Thunder"] = "/images/logos/thunder.png",
                    ["Magic"] = "/images/logos/magic.png",
                    ["76ers"] = "/images/logos/Philadelphia.png",
                    ["Suns"] = "/images/logos/Phoenix Suns.png",
                    ["Trail Blazers"] = "/images/logos/Portland Trail Blazers.png",
                    ["Kings"] = "/images/logos/Sacramento Kings.png",
                    ["Spurs"] = "/images/logos/Spurs.png",
                    ["Raptors"] = "/images/logos/Raptors.png",
                    ["Wizards"] = "/images/logos/Washington Wizards.png",
                    ["Jazz"] = "/images/logos/jazz.png",

                    // EuroLeague (alignés avec tes fichiers)
                    ["Anadolu Efes Istanbul"] = "/images/logos/istanbul.png",
                    ["Monaco"] = "/images/logos/monaco.png",
                    ["Victoria"] = "/images/logos/victoria.png",
                    ["Meridians"] = "/images/logos/meridians.png",
                    ["Dubai"] = "/images/logos/dubai.png",
                    ["Emporio"] = "/images/logos/emporio.png",
                    ["Barcelona"] = "/images/logos/barcelona.png",
                    ["Bayern Munich"] = "/images/logos/bayernm.png",
                    ["Fenerbahce Beko"] = "/images/logos/fenerbahce.png",
                    ["Hapoel Jerusalem"] = "/images/logos/jerusalem.png",
                    ["ASVEL"] = "/images/logos/asvel.png",
                    ["RAPYD"] = "/images/logos/rapyd.png",
                    ["Olympiacos Piraeus"] = "/images/logos/piraeus.png",
                    ["Aktor"] = "/images/logos/aktora.png",
                    ["Paris Basket"] = "/images/logos/paris.png",
                    ["Partizan Mozzart"] = "/images/logos/partizan.png",
                    ["Real Madrid"] = "/images/logos/realmadrid.png",
                    ["Valencia"] = "/images/logos/valencia.png",
                    ["Virtus"] = "/images/logos/virtus.png",
                    ["Zalgiris"] = "/images/logos/zalgiris.png",
                };
                var heroMap = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Hawks"] = "/images/team-bg/hawks.png",
                    ["Celtics"] = "/images/team-bg/celtics.png",
                    ["Nets"] = "/images/team-bg/nets.png",
                    ["Hornets"] = "/images/team-bg/hornets.png",
                    ["Bulls"] = "/images/team-bg/bulls.png",
                    ["Cavaliers"] = "/images/team-bg/cavaliers.png",
                    ["Mavericks"] = "/images/team-bg/maverick.png",
                    ["Nuggets"] = "/images/team-bg/nuggets.png",
                    ["Pistons"] = "/images/team-bg/pistons.png",
                    ["Warriors"] = "/images/team-bg/warriors.png",
                    ["Rockets"] = "/images/team-bg/rockets.png",
                    ["Pacers"] = "/images/team-bg/pacers.png",
                    ["Clippers"] = "/images/team-bg/clippers.png",
                    ["Lakers"] = "/images/team-bg/lakers.png",
                    ["Grizzlies"] = "/images/team-bg/grizzlies.png",
                    ["Heat"] = "/images/team-bg/heat.png",
                    ["Bucks"] = "/images/team-bg/bucks.png", // d'après ta capture
                    ["Timberwolves"] = "/images/team-bg/timberwolves.png",
                    ["Pelicans"] = "/images/team-bg/pelicans.png",
                    ["Knicks"] = "/images/team-bg/knicks.png",
                    ["Thunder"] = "/images/team-bg/thunder.png",
                    ["Magic"] = "/images/team-bg/magic.png",
                    ["76ers"] = "/images/team-bg/76ers.png",
                    ["Suns"] = "/images/team-bg/suns.png",
                    ["Trail Blazers"] = "/images/team-bg/blazers.png",
                    ["Kings"] = "/images/team-bg/kings.png",
                    ["Spurs"] = "/images/team-bg/spurs.png",
                    ["Raptors"] = "/images/team-bg/raptors.png",
                    ["Wizards"] = "/images/team-bg/wizards.png",
                    ["Jazz"] = "/images/team-bg/jazz.png",
                };

                var teams = await ctx.Teams.ToListAsync();
                var changed = false;

                foreach (var t in teams)
                {
                    if (logoMap.TryGetValue(t.Name, out var logo) &&
                        !string.Equals(t.LogoUrl, logo, StringComparison.OrdinalIgnoreCase))
                    {
                        t.LogoUrl = logo;
                        changed = true;
                    }

                    if (heroMap.TryGetValue(t.Name, out var hero) &&
                        !string.Equals(t.HeroImageUrl, hero, StringComparison.OrdinalIgnoreCase))
                    {
                        t.HeroImageUrl = hero;
                        changed = true;
                    }
                }

                if (changed) await ctx.SaveChangesAsync();
            }

            // Rôles
            if (!await roleMgr.RoleExistsAsync("admin"))
                await roleMgr.CreateAsync(new IdentityRole("admin"));
            if (!await roleMgr.RoleExistsAsync("user"))
                await roleMgr.CreateAsync(new IdentityRole("user"));

            // Admin par défaut
            var admin = await userMgr.FindByEmailAsync("admin@basket.test");
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = "admin@basket.test",
                    Email = "admin@basket.test",
                    FullName = "Admin"
                };
                await userMgr.CreateAsync(admin, "Admin123!");
                await userMgr.AddToRolesAsync(admin, new[] { "admin", "user" });
            }

            // Création initiale des ligues/équipes/offres si vide
            if (!await ctx.Leagues.AnyAsync())
            {
                var nba = new League { Name = "NBA" };
                var euro = new League { Name = "EuroLeague" };
                ctx.Leagues.AddRange(nba, euro);

                var hawks        = new Team { Name = "Hawks", League = nba, LogoUrl = "/images/logos/atlanta.png" };
                var celtics      = new Team { Name = "Celtics",      League = nba, LogoUrl = "/images/logos/celtix.png", HeroImageUrl = "/images/team-bg/celtics.png" };
                var nets         = new Team { Name = "Nets",         League = nba, LogoUrl = "/images/logos/nets.png", HeroImageUrl= "/images/team-bg/nets.png" };
                var hornets      = new Team { Name = "Hornets",      League = nba, LogoUrl = "/images/logos/hornets.png", HeroImageUrl= "/images/team-bg/hornets.png" };
                var bulls        = new Team { Name = "Bulls",        League = nba, LogoUrl = "/images/logos/chicago.png", HeroImageUrl = "/images/team-bg/bulls.png" };
                var cavaliers    = new Team { Name = "Cavaliers",    League = nba, LogoUrl = "/images/logos/clevland.png", HeroImageUrl = "/images/team-bg/cavaliers.png" };
                var mavs         = new Team { Name = "Mavericks",    League = nba, LogoUrl = "/images/logos/Mavericks.png", HeroImageUrl = "/images/team-bg/mavs.png" };
                var nuggets      = new Team { Name = "Nuggets",      League = nba, LogoUrl = "/images/logos/Nuggets.png", HeroImageUrl = "/images/team-bg/nuggets.png" };
                var pistons      = new Team { Name = "Pistons",      League = nba, LogoUrl = "/images/logos/detroit_piston.png", HeroImageUrl = "/images/team-bg/pistons.png" };
                var warriors     = new Team { Name = "Warriors",     League = nba, LogoUrl = "/images/logos/warriors.png", HeroImageUrl = "/images/team-bg/warriors.png" };
                var rockets      = new Team { Name = "Rockets",      League = nba, LogoUrl = "/images/logos/rokets.png", HeroImageUrl = "/images/team-bg/rockets.png" };
                var pacers       = new Team { Name = "Pacers",       League = nba, LogoUrl = "/images/logos/Pacers.png", HeroImageUrl = "/images/team-bg/pacers.png" };
                var clippers     = new Team { Name = "Clippers",     League = nba, LogoUrl = "/images/logos/Clippers.png", HeroImageUrl = "/images/team-bg/clippers.png" };
                var lakers       = new Team { Name = "Lakers",       League = nba, LogoUrl = "/images/logos/Lakers.png", HeroImageUrl = "/images/team-bg/lakers.png" };
                var grizzlies    = new Team { Name = "Grizzlies",    League = nba, LogoUrl = "/images/logos/grizzlies.png", HeroImageUrl = "/images/team-bg/grizzlies.png" };
                var heat         = new Team { Name = "Heat",         League = nba, LogoUrl = "/images/logos/miami.png", HeroImageUrl = "/images/team-bg/heat.png" };
                var bucks        = new Team { Name = "Bucks",        League = nba, LogoUrl = "/images/logos/Buks.png", HeroImageUrl = "/images/team-bg/bucks.png" };
                var timberwolves = new Team { Name = "Timberwolves", League = nba, LogoUrl = "/images/logos/timberWolves.png", HeroImageUrl = "/images/team-bg/timberwolves.png" };
                var jazz        = new Team { Name = "Jazz",         League = nba, LogoUrl = "/images/logos/Jazz.png", HeroImageUrl = "/images/team-bg/jazz.png" };
                var pelicans     = new Team { Name = "Pelicans",     League = nba, LogoUrl = "/images/logos/neworleans.png", HeroImageUrl = "/images/team-bg/pelicans.png" };
                var knicks       = new Team { Name = "Knicks",       League = nba, LogoUrl = "/images/logos/newyork.png", HeroImageUrl = "/images/team-bg/knicks.png" };
                var thunder      = new Team { Name = "Thunder",      League = nba, LogoUrl = "/images/logos/thunder.png", HeroImageUrl = "/images/team-bg/thunder.png" };
                var magic        = new Team { Name = "Magic",        League = nba, LogoUrl = "/images/logos/magic.png", HeroImageUrl = "/images/team-bg/magic.png" };
                var ers          = new Team { Name = "76ers",        League = nba, LogoUrl = "/images/logos/Philadelphia.png", HeroImageUrl = "/images/team-bg/76ers.png" };
                var suns         = new Team { Name = "Suns",         League = nba, LogoUrl = "/images/logos/Phoenix Suns.png", HeroImageUrl = "/images/team-bg/suns.png" };
                var blazers      = new Team { Name = "Trail Blazers",      League = nba, LogoUrl = "/images/logos/Portland Trail Blazers.png", HeroImageUrl = "/images/team-bg/blazers.png" };
                var kings        = new Team { Name = "Kings",        League = nba, LogoUrl = "/images/logos/Sacramento Kings.png", HeroImageUrl = "/images/team-bg/kings.png" };
                var spurs        = new Team { Name = "Spurs",        League = nba, LogoUrl = "/images/logos/Spurs.png", HeroImageUrl = "/images/team-bg/spurs.png" };
                var raptors      = new Team { Name = "Raptors",      League = nba, LogoUrl = "/images/logos/Raptors.png", HeroImageUrl = "/images/team-bg/raptors.png" };
                var wizards      = new Team { Name = "Wizards",      League = nba, LogoUrl = "/images/logos/Washington Wizards.png", HeroImageUrl = "/images/team-bg/wizards.png" };

                var anadoluefesistanbul = new Team { Name = "Anadolu Efes Istanbul", League = euro, LogoUrl = "/images/logos/anadolu.png" };
                var monaco              = new Team { Name = "Monaco",                League = euro, LogoUrl = "/images/logos/monaco.png" };
                var victoria            = new Team { Name = "Victoria",              League = euro, LogoUrl = "/images/logos/Saski_Baskonia.png" };
                var meridians           = new Team { Name = "Meridians",             League = euro, LogoUrl = "/images/logos/KK_Crvena_zvezda_logo.svg.png" };
                var dubai               = new Team { Name = "Dubai",                 League = euro, LogoUrl = "/images/logos/Dubai_BC_Logo.png" };
                var emporio             = new Team { Name = "Emporio",               League = euro, LogoUrl = "/images/logos/milan.png" };
                var barcelona           = new Team { Name = "Barcelona",             League = euro, LogoUrl = "/images/logos/FC_Barcelona.png" };
                var bayernmunich        = new Team { Name = "Bayern Munich",         League = euro, LogoUrl = "/images/logos/bayern.png" };
                var fenerbahcebeko      = new Team { Name = "Fenerbahce Beko",       League = euro, LogoUrl = "/images/logos/Fenerbahçe_Men's_Basketball_logo.svg.png" };
                var hapoeljerusalem     = new Team { Name = "Hapoel Jerusalem",      League = euro, LogoUrl = "/images/logos/Hapoel.png" };
                var asvel               = new Team { Name = "ASVEL",                 League = euro, LogoUrl = "/images/logos/ldlc-asvel-lyon-villeurbanne.png" };
                var rapyd               = new Team { Name = "RAPYD",                 League = euro, LogoUrl = "/images/logos/Maccabi.png" };
                var piraeus             = new Team { Name = "Olympiacos Piraeus",    League = euro, LogoUrl = "/images/logos/Olympiacos.png" };
                var aktora              = new Team { Name = "Aktor",                 League = euro, LogoUrl = "/images/logos/panathinaikos.png" };
                var parisbasket         = new Team { Name = "Paris Basket",          League = euro, LogoUrl = "/images/logos/paris.png" };
                var partizanmozzart     = new Team { Name = "Partizan Mozzart",      League = euro, LogoUrl = "/images/logos/partizan.png" };
                var real                = new Team { Name = "Real Madrid",           League = euro, LogoUrl = "/images/logos/Logo_Real_Madrid.png" };
                var valencia            = new Team { Name = "Valencia",              League = euro, LogoUrl = "/images/logos/Valencia.png" };
                var virtus              = new Team { Name = "Virtus",                League = euro, LogoUrl = "/images/logos/Virtus_Bologna.png" };
                var zalgiris            = new Team { Name = "Zalgiris",              League = euro, LogoUrl = "/images/logos/Zalgiris.png" };

                ctx.Teams.AddRange(
                    hawks, celtics, nets, hornets, bulls, cavaliers, mavs, nuggets, pistons, warriors, rockets,
                    pacers, clippers, lakers, grizzlies, heat, bucks, timberwolves, pelicans, knicks, thunder, magic,
                    ers, suns, blazers, kings, spurs, raptors, wizards,jazz,
                    anadoluefesistanbul, monaco, victoria, meridians, dubai, emporio, barcelona, bayernmunich,
                    fenerbahcebeko, hapoeljerusalem, asvel, rapyd, piraeus, aktora, parisbasket, partizanmozzart,
                    real, valencia, virtus, zalgiris
                );

                var std = new SeatCategory { Name = "Standard" };
                var prem = new SeatCategory { Name = "Premium" };
                var or = new SeatCategory { Name = "Or" };
                ctx.SeatCategories.AddRange(std, prem, or);

                await ctx.SaveChangesAsync();
            }
        }
    }
}
