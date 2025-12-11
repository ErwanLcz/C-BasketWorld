using System.Globalization;
using BasketWorld.Data;
using BasketWorld.Models;
using Microsoft.EntityFrameworkCore;

namespace BasketWorld.Services
{
    public class EuroleagueOfficialSyncService
    {
        private readonly ApplicationDbContext _ctx;
        private readonly EuroleagueOfficialClient _api;

        private const string EuroleagueName = "EuroLeague";

        public EuroleagueOfficialSyncService(ApplicationDbContext ctx, EuroleagueOfficialClient api)
        {
            _ctx = ctx;
            _api = api;
        }   

        private async Task<League> GetOrCreateEuroleagueAsync()
        {
            var league = await _ctx.Leagues.FirstOrDefaultAsync(l => l.Name == EuroleagueName);
            if (league != null) return league;

            league = new League { Name = EuroleagueName };
            _ctx.Leagues.Add(league);
            await _ctx.SaveChangesAsync();
            return league;
        }

        // ──────────────────────────────
        // Helpers
        // ──────────────────────────────

        private static DateTime? ParseGameDateTimeUtc(ScheduleItem s)
        {
            if (string.IsNullOrWhiteSpace(s.DateRaw) ||
                string.IsNullOrWhiteSpace(s.StartTimeRaw))
                return null;

            // Exemple dans ton XML : "Sep 30, 2025" + "20:45"
            var toParse = $"{s.DateRaw} {s.StartTimeRaw}";

            if (!DateTime.TryParseExact(
                    toParse,
                    "MMM dd, yyyy HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out var local))
            {
                return null;
            }

            return local.ToUniversalTime();
        }

        /// <summary>
        /// S’assure qu’une équipe existe (par nom / code court) pendant la PASSE 1.
        /// Ne fait que créer des Team et les enregistrer dans les dictionnaires,
        /// on ne touche pas encore aux Games.
        /// </summary>
        private Team EnsureTeamFromSchedule(
            League league,
            string? name,
            string? shortCode,
            Dictionary<string, Team> teamsByName,
            Dictionary<string, Team> teamsByCode,
            ref int createdCount)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("EnsureTeamFromSchedule called with empty name");

            Team? team = null;

            // 1) On tente par code d'abord
            if (!string.IsNullOrWhiteSpace(shortCode) &&
                teamsByCode.TryGetValue(shortCode, out var byCode))
            {
                team = byCode;
            }

            // 2) Sinon par nom
            if (team == null &&
                teamsByName.TryGetValue(name, out var byName))
            {
                team = byName;
            }

            if (team != null)
            {
                // Si on découvre un code court et qu’on ne l’avait pas encore, on le renseigne
                if (!string.IsNullOrWhiteSpace(shortCode) &&
                    string.IsNullOrWhiteSpace(team.Abbreviation))
                {
                    team.Abbreviation = shortCode;
                    teamsByCode[shortCode] = team;
                }

                return team;
            }

            // 3) Création d’une nouvelle équipe
            team = new Team
            {
                LeagueId = league.Id,
                Name = name,
                Abbreviation = shortCode
            };

            _ctx.Teams.Add(team);
            createdCount++;

            teamsByName[name] = team;
            if (!string.IsNullOrWhiteSpace(shortCode))
                teamsByCode[shortCode] = team;

            Console.WriteLine($"[EuroLeagueOfficial] CREATED team: name='{name}', code='{shortCode}'");

            return team;
        }

        // ──────────────────────────────
        // Sync principale
        // ──────────────────────────────

        public async Task<int> SyncSeasonAsync(string seasonCode)
        {
            var league = await GetOrCreateEuroleagueAsync();

            // 1) Appel API XML → tous les matchs de la saison
            var rawGames = await _api.GetSeasonScheduleAsync(seasonCode);
            Console.WriteLine($"[EuroLeagueOfficial] Reçu {rawGames.Count} games pour {seasonCode}");

            // ───────────
            // PASSE 1 : s’assurer que toutes les équipes existent
            // ───────────

            var existingTeams = await _ctx.Teams
                .Where(t => t.LeagueId == league.Id)
                .ToListAsync();

            var teamsByName = existingTeams
                .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var teamsByCode = existingTeams
                .Where(t => !string.IsNullOrWhiteSpace(t.Abbreviation))
                .GroupBy(t => t.Abbreviation!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var createdTeamsFromSchedule = 0;

            foreach (var s in rawGames)
            {
                if (string.IsNullOrWhiteSpace(s.HomeTeamName) ||
                    string.IsNullOrWhiteSpace(s.AwayTeamName))
                {
                    continue;
                }

                // Home
                EnsureTeamFromSchedule(
                    league,
                    s.HomeTeamName,
                    s.HomeTeamCode,
                    teamsByName,
                    teamsByCode,
                    ref createdTeamsFromSchedule
                );

                // Away
                EnsureTeamFromSchedule(
                    league,
                    s.AwayTeamName,
                    s.AwayTeamCode,
                    teamsByName,
                    teamsByCode,
                    ref createdTeamsFromSchedule
                );
            }

            if (createdTeamsFromSchedule > 0)
            {
                await _ctx.SaveChangesAsync(); // INSERT Teams d’abord
                Console.WriteLine($"[EuroLeagueOfficial] Créé {createdTeamsFromSchedule} équipes à partir du schedule.");
            }

            // ───────────
            // PASSE 2 : rechargement des équipes + upsert des Games
            // ───────────

            existingTeams = await _ctx.Teams
                .Where(t => t.LeagueId == league.Id)
                .ToListAsync();

            teamsByName = existingTeams
                .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            teamsByCode = existingTeams
                .Where(t => !string.IsNullOrWhiteSpace(t.Abbreviation))
                .GroupBy(t => t.Abbreviation!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            // Clé des games existants : (Season, ExternalId) où ExternalId = GameNumber
            var existingGames = await _ctx.Games
                .Where(g => g.LeagueId == league.Id && g.Source == "euroleague_official")
                .ToListAsync();

            var gamesByKey = existingGames.ToDictionary(
                g => (Season: g.Season, ExternalId: g.ExternalId ?? 0),
                g => g
            );

            var upserts = 0;
            var skippedNoTeams = 0;
            var skippedNoDate = 0;

            foreach (var s in rawGames)
            {
                if (string.IsNullOrWhiteSpace(s.HomeTeamName) ||
                    string.IsNullOrWhiteSpace(s.AwayTeamName))
                {
                    skippedNoTeams++;
                    Console.WriteLine($"[EuroLeagueOfficial] SKIP no teams for gamecode={s.GameCode}");
                    continue;
                }

                // Résolution équipes (code en priorité, sinon nom)
                Team? home = null;
                Team? away = null;

                if (!string.IsNullOrWhiteSpace(s.HomeTeamCode) &&
                    teamsByCode.TryGetValue(s.HomeTeamCode, out var homeByCode))
                {
                    home = homeByCode;
                }
                else if (teamsByName.TryGetValue(s.HomeTeamName, out var homeByName))
                {
                    home = homeByName;
                }

                if (!string.IsNullOrWhiteSpace(s.AwayTeamCode) &&
                    teamsByCode.TryGetValue(s.AwayTeamCode, out var awayByCode))
                {
                    away = awayByCode;
                }
                else if (teamsByName.TryGetValue(s.AwayTeamName, out var awayByName))
                {
                    away = awayByName;
                }

                if (home == null || away == null)
                {
                    skippedNoTeams++;
                    Console.WriteLine($"[EuroLeagueOfficial] SKIP mapping teams: gamecode={s.GameCode}, home='{s.HomeTeamName}'({s.HomeTeamCode}), away='{s.AwayTeamName}'({s.AwayTeamCode})");
                    continue;
                }

                // Date / heure
                var dtUtc = ParseGameDateTimeUtc(s);
                if (dtUtc == null)
                {
                    skippedNoDate++;
                    Console.WriteLine($"[EuroLeagueOfficial] WARN date null: gamecode={s.GameCode} date='{s.DateRaw}' time='{s.StartTimeRaw}'");
                    // on peut choisir de SKIP ou de mettre une date par défaut
                    // ici on SKIP pour éviter des matchs mal positionnés
                    continue;
                }

                // Saison numérique à partir de "E2025_7" -> 2025
                var seasonYear = 0;
                if (!string.IsNullOrWhiteSpace(s.GameCode) &&
                    s.GameCode.Length >= 5 &&
                    int.TryParse(s.GameCode.Substring(1, 4), out var parsedYear))
                {
                    seasonYear = parsedYear;
                }

                var extId = s.GameNumber; // identifiant "logique" du match dans la saison
                var key = (Season: seasonYear, ExternalId: extId);

                if (!gamesByKey.TryGetValue(key, out var game))
                {
                    game = new Game
                    {
                        LeagueId   = league.Id,
                        Season     = seasonYear,
                        ExternalId = extId,
                        Source     = "euroleague_official",
                        HomeTeamId = home.Id,
                        AwayTeamId = away.Id,
                        StartAt    = dtUtc.Value,
                        Venue      = s.ArenaName ?? ""
                    };
                    _ctx.Games.Add(game);
                    upserts++;
                }
                else
                {
                    game.HomeTeamId = home.Id;
                    game.AwayTeamId = away.Id;
                    game.StartAt    = dtUtc.Value;
                    game.Venue      = s.ArenaName ?? game.Venue;
                }

                // ──────────────────────────────
                // 🔥 Récupération des scores si le match est joué
                // ──────────────────────────────
                if (s.Played)
                {
                    try
                    {
                        // s.GameNumber = 7 dans ton XML de schedule
                        var result = await _api.GetGameResultAsync(seasonCode, s.GameNumber);

                        if (result != null)
                        {
                            game.HomeScore = result.HomeScore;
                            game.AwayScore = result.AwayScore;
                            game.Status    = result.Status ?? "Final";
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[EuroLeagueOfficial] WARN GetGameResult failed for season={seasonCode}, game={s.GameNumber}: {ex.Message}");
                    }
                }

            }

            await _ctx.SaveChangesAsync();

            Console.WriteLine(
                $"[EuroLeagueOfficial] Fin sync {seasonCode} -> upserts={upserts}, " +
                $"skippedNoTeams={skippedNoTeams}, skippedNoDate={skippedNoDate}");

            return upserts;
        }
    }
}
