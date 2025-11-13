using BasketWorld.Data;
using BasketWorld.Models;
using Microsoft.EntityFrameworkCore;

namespace BasketWorld.Services
{
    public class NbaSyncService
    {
        private readonly ApplicationDbContext _ctx;
        private readonly BalldontlieClient _api;

        // Petit cache des équipes BL pour éviter des appels répétitifs
        private List<BlTeam>? _blTeamsCache;

        public NbaSyncService(ApplicationDbContext ctx, BalldontlieClient api)
        {
            _ctx = ctx;
            _api = api;
        }

        /// <summary>
        /// Sync global (équipes puis matchs) sur une fenêtre UTC.
        /// Retourne (teamsUpserts, gamesUpserts).
        /// </summary>
        public async Task<(int teams, int games)> SyncAsync(DateTime fromUtc, DateTime toUtc)
        {
            if (fromUtc.Kind != DateTimeKind.Utc || toUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("fromUtc/toUtc doivent être en UTC.");

            var teams = await SyncTeamsAsync();
            var games = await SyncGamesAsync(fromUtc, toUtc);
            return (teams, games);
        }

        /// <summary>
        /// Assure l’existence de la ligue "NBA" (clé = Name).
        /// </summary>
        private async Task<League> GetOrCreateNbaAsync()
        {
            var nba = await _ctx.Leagues.FirstOrDefaultAsync(l => l.Name == "NBA");
            if (nba != null) return nba;

            nba = new League { Name = "NBA" };
            _ctx.Leagues.Add(nba);
            await _ctx.SaveChangesAsync();
            return nba;
        }

        /// <summary>
        /// Récupère (et met en cache) la liste des équipes balldontlie.
        /// </summary>
        private async Task<List<BlTeam>> GetBlTeamsAsync()
        {
            if (_blTeamsCache != null) return _blTeamsCache;
            _blTeamsCache = await _api.GetTeamsAsync();
            return _blTeamsCache;
        }

        /// <summary>
        /// Upsert des équipes NBA.
        /// </summary>
        public async Task<int> SyncTeamsAsync()
        {
            var nba = await GetOrCreateNbaAsync();
            var blTeams = await GetBlTeamsAsync();

            var teamsByExt = await _ctx.Teams
                .Where(t => t.LeagueId == nba.Id && t.ExternalId != null)
                .ToDictionaryAsync(t => t.ExternalId!.Value);

            var teamsByName = (await _ctx.Teams
                    .Where(t => t.LeagueId == nba.Id)
                    .ToListAsync())
                .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var upserts = 0;

            foreach (var bt in blTeams)
            {
                if (teamsByExt.TryGetValue(bt.Id, out var tByExt))
                {
                    tByExt.Name = bt.Name;
                    tByExt.Abbreviation = bt.Abbreviation;
                    _ctx.Teams.Update(tByExt);
                    upserts++;
                }
                else if (teamsByName.TryGetValue(bt.Name, out var tByName))
                {
                    tByName.ExternalId = bt.Id;
                    tByName.Abbreviation = bt.Abbreviation;
                    _ctx.Teams.Update(tByName);
                    upserts++;
                }
                else
                {
                    var created = new Team
                    {
                        League = nba,
                        Name = bt.Name,
                        Abbreviation = bt.Abbreviation,
                        ExternalId = bt.Id
                    };
                    _ctx.Teams.Add(created);
                    upserts++;
                }
            }

            await _ctx.SaveChangesAsync();
            return upserts;
        }

        /// <summary>
        /// Upsert des matchs sur une fenêtre temporelle UTC.
        /// </summary>
        public async Task<int> SyncGamesAsync(DateTime fromUtc, DateTime toUtc)
        {
            var nba = await GetOrCreateNbaAsync();
            var blTeams = await GetBlTeamsAsync();

            var teamsByExt = await _ctx.Teams
                .Where(t => t.LeagueId == nba.Id && t.ExternalId != null)
                .ToDictionaryAsync(t => t.ExternalId!.Value);

            var blGames = await _api.GetGamesByDateRangeAsync(fromUtc, toUtc);

            var upserts = 0;

            foreach (var g in blGames)
            {
                // Associer les équipes
                if (!teamsByExt.TryGetValue(g.Home_team.Id, out var home) ||
                    !teamsByExt.TryGetValue(g.Visitor_team.Id, out var away))
                {
                    // Si on n'a pas encore mappé l'équipe, on saute ce match (ou on pourrait forcer un resync teams).
                    continue;
                }

                var status = (g.Status ?? string.Empty).Trim();
                // balldontlie utilise "Final", "Final/OT", etc. -> on détecte la présence de "Final"
                bool isFinal = status.IndexOf("final", StringComparison.OrdinalIgnoreCase) >= 0;

                // ➜ RÈGLE SCORE :
                // - Final -> on persiste tel quel
                // - Non-final mais (home>0 || away>0) -> on persiste (match en cours)
                // - 0–0 non-final -> null (éviter d'afficher un faux 0–0 pour un match à venir)
                int? mappedHome = (isFinal || g.Home_team_score > 0 || g.Visitor_team_score > 0) ? g.Home_team_score : (int?)null;
                int? mappedAway = (isFinal || g.Home_team_score > 0 || g.Visitor_team_score > 0) ? g.Visitor_team_score : (int?)null;

                var startAtUtc = DateTime.SpecifyKind(g.Date, DateTimeKind.Utc);

                var existing = await _ctx.Games
                    .FirstOrDefaultAsync(x => x.Source == "balldontlie" && x.ExternalId == g.Id);

                if (existing == null)
                {
                    var game = new Game
                    {
                        League = nba,
                        HomeTeam = home,
                        AwayTeam = away,

                        StartAt = startAtUtc,
                        Venue = "", // pas d'info d'arène dispo -> vide (non-nullable)

                        ExternalId = g.Id,
                        Source = "balldontlie",
                        Season = g.Season,
                        Status = status,

                        HomeScore = mappedHome,
                        AwayScore = mappedAway
                    };

                    _ctx.Games.Add(game);
                    upserts++;
                }
                else
                {
                    existing.HomeTeamId = home.Id;
                    existing.AwayTeamId = away.Id;

                    existing.StartAt = startAtUtc;
                    if (existing.Venue == null) existing.Venue = "";

                    existing.Season = g.Season;
                    existing.Status = status;

                    existing.HomeScore = mappedHome;
                    existing.AwayScore = mappedAway;

                    _ctx.Games.Update(existing);
                    upserts++;
                }
            }

            await _ctx.SaveChangesAsync();
            return upserts;
        }
    }
}
