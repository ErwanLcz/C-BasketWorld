using BasketWorld.Data;
using BasketWorld.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BasketWorld.Services
{
    public class StandingsService
    {
        private readonly ApplicationDbContext _ctx;
        public StandingsService(ApplicationDbContext ctx) => _ctx = ctx;

        public async Task<List<StandingRowVM>> GetStandingsAsync(int leagueId, int season)
        {
            var games = await _ctx.Games
                .AsNoTracking()
                .Include(g => g.HomeTeam)
                .Include(g => g.AwayTeam)
                .Where(g => g.LeagueId == leagueId
                         && g.Season == season
                         && g.HomeScore.HasValue
                         && g.AwayScore.HasValue
                         && (g.Status == "Final" || g.Status == "Finished" || g.Status == "FT"))
                .ToListAsync();

            // ✅ On ne crée des rows que pour les équipes qui apparaissent dans au moins 1 match
            var map = new Dictionary<int, StandingRowVM>();

            StandingRowVM EnsureTeam(int teamId, string name, string? logoUrl)
            {
                if (!map.TryGetValue(teamId, out var row))
                {
                    row = new StandingRowVM
                    {
                        TeamId = teamId,
                        TeamName = name,
                        LogoUrl = logoUrl
                    };
                    map[teamId] = row;
                }
                return row;
            }

            foreach (var g in games)
            {
                var home = EnsureTeam(g.HomeTeamId, g.HomeTeam.Name, g.HomeTeam.LogoUrl);
                var away = EnsureTeam(g.AwayTeamId, g.AwayTeam.Name, g.AwayTeam.LogoUrl);

                home.Played++;
                away.Played++;

                int hs = g.HomeScore!.Value;
                int @as = g.AwayScore!.Value;

                home.PointsFor += hs;
                home.PointsAgainst += @as;
                away.PointsFor += @as;
                away.PointsAgainst += hs;

                if (hs > @as) { home.Wins++; away.Losses++; }
                else if (@as > hs) { away.Wins++; home.Losses++; }
            }

            return map.Values
                .OrderByDescending(x => x.WinPct)
                .ThenByDescending(x => x.Diff)
                .ThenByDescending(x => x.PointsFor)
                .ToList();
        }
    }
}
