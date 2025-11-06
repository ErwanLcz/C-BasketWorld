using BasketWorld.Data;
using BasketWorld.Models;
using Microsoft.EntityFrameworkCore;

namespace BasketWorld.Services
{
    public class NbaSyncService
    {
        private readonly ApplicationDbContext _ctx;
        private readonly BalldontlieClient _api;
        private List<BlTeam>? _blTeamsCache;


        public NbaSyncService(ApplicationDbContext ctx, BalldontlieClient api)
        {
            _ctx = ctx;
            _api = api;
        }

        public async Task<int> SyncAsync(DateTime fromUtc, DateTime toUtc)
        {
            // 1) Assure la ligue NBA
            var nba = await _ctx.Leagues.FirstOrDefaultAsync(l => l.Name == "NBA");
            if (nba == null)
            {
                nba = new League { Name = "NBA" };
                _ctx.Leagues.Add(nba);
                await _ctx.SaveChangesAsync();
            }

            // 2) Upsert Teams
            // Simple cache mémoire par exécution
            if (_blTeamsCache == null) _blTeamsCache = await _api.GetTeamsAsync();
            var blTeams = _blTeamsCache;

            // NE GARDER que celles qui ont un ExternalId pour le dico par id externe
            var teamsByExt = await _ctx.Teams
                .Where(t => t.LeagueId == nba.Id && t.ExternalId != null)
                .ToDictionaryAsync(t => t.ExternalId!.Value);

            // Dico par nom (insensible à la casse)
            var teamsByNameList = await _ctx.Teams
                .Where(t => t.LeagueId == nba.Id)
                .ToListAsync();

            var teamsByName = teamsByNameList
                .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var bt in blTeams)
            {
                Team team;
                if (teamsByExt.TryGetValue(bt.Id, out team!))
                {
                    // update
                    team.Name = bt.Name; // "Lakers"
                    team.Abbreviation = bt.Abbreviation; // "LAL"
                }
                else if (teamsByName.TryGetValue(bt.Name, out team!))
                {
                    // link external id
                    team.ExternalId = bt.Id;
                    team.Abbreviation = bt.Abbreviation;
                }
                else
                {
                    team = new Team
                    {
                        League = nba,
                        Name = bt.Name,
                        ExternalId = bt.Id,
                        Abbreviation = bt.Abbreviation,
                        // LogoUrl: tu peux mapper une table de correspondance ici si tu veux
                    };
                    _ctx.Teams.Add(team);
                }
            }
            await _ctx.SaveChangesAsync();

            // Reconstitue un dico (après insert)
            var byExt = await _ctx.Teams.Where(t => t.LeagueId == nba.Id && t.ExternalId != null)
                                        .ToDictionaryAsync(t => t.ExternalId!.Value);

            // 3) Upsert Games (fenêtre from..to)
            var blGames = await _api.GetGamesByDateRangeAsync(fromUtc, toUtc);
            System.Diagnostics.Debug.WriteLine($"[NBA SYNC] API returned {blGames.Count} games for {fromUtc:yyyy-MM-dd} -> {toUtc:yyyy-MM-dd}");
            int upserts = 0;

            foreach (var g in blGames)
            {
                if (!byExt.TryGetValue(g.Home_team.Id, out var home) || !byExt.TryGetValue(g.Visitor_team.Id, out var away))
                    continue; // sécurité

                var existing = await _ctx.Games
                    .FirstOrDefaultAsync(x => x.Source == "balldontlie" && x.ExternalId == g.Id);

                if (existing == null)
                {
                    existing = new Game
                    {
                        League = nba,
                        HomeTeam = home,
                        AwayTeam = away,
                        StartAt = g.Date,         // g.Date est UTC
                        Venue = "",               // balldontlie n’a pas le stade; laisse vide
                        ExternalId = g.Id,
                        Source = "balldontlie",
                        Season = g.Season,
                        Status = g.Status,
                        HomeScore = g.Home_team_score == 0 ? null : g.Home_team_score,
                        AwayScore = g.Visitor_team_score == 0 ? null : g.Visitor_team_score
                    };
                    _ctx.Games.Add(existing);
                    upserts++;
                }
                else
                {
                    existing.HomeTeamId = home.Id;
                    existing.AwayTeamId = away.Id;
                    existing.StartAt = g.Date;
                    existing.Season = g.Season;
                    existing.Status = g.Status;
                    existing.HomeScore = g.Home_team_score == 0 ? null : g.Home_team_score;
                    existing.AwayScore = g.Visitor_team_score == 0 ? null : g.Visitor_team_score;
                    _ctx.Games.Update(existing);
                    upserts++;
                }
            }

            await _ctx.SaveChangesAsync();
            return upserts;
        }
    }
}
