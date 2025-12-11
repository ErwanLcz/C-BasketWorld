using BasketWorld.Data;
using BasketWorld.Models;
using Microsoft.EntityFrameworkCore;

namespace BasketWorld.Services
{
    public class EuroleagueSyncService
    {
        private readonly ApplicationDbContext _ctx;
        private readonly TheSportsDbClient _api;

        private const string EuroleagueName = "EuroLeague Basketball";

        public EuroleagueSyncService(ApplicationDbContext ctx, TheSportsDbClient api)
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

        /// <summary>
        /// Upsert des équipes EuroLeague à partir de l’API TheSportsDB.
        /// </summary>
        public async Task<int> SyncTeamsAsync()
        {
            var league = await GetOrCreateEuroleagueAsync();
            var tsTeams = await _api.GetEuroleagueTeamsAsync();

            var existingByExt = await _ctx.Teams
                .Where(t => t.LeagueId == league.Id && t.ExternalId != null)
                .ToDictionaryAsync(t => t.ExternalId!.Value);

            var existingByName = (await _ctx.Teams
                    .Where(t => t.LeagueId == league.Id)
                    .ToListAsync())
                .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var upserts = 0;

            foreach (var ts in tsTeams)
            {
                if (string.IsNullOrWhiteSpace(ts.StrTeam))
                    continue;

                int? extId = null;
                if (int.TryParse(ts.IdTeam, out var parsed))
                    extId = parsed;

                Team? team = null;

                if (extId.HasValue && existingByExt.TryGetValue(extId.Value, out var byExt))
                    team = byExt;
                else if (existingByName.TryGetValue(ts.StrTeam, out var byName))
                    team = byName;

                if (team == null)
                {
                    team = new Team
                    {
                        LeagueId = league.Id,
                        Name = ts.StrTeam
                    };
                    _ctx.Teams.Add(team);
                    upserts++;
                }

                team.ExternalId = extId;
                team.Abbreviation = string.IsNullOrWhiteSpace(ts.StrTeamShort)
                    ? null
                    : ts.StrTeamShort;

                // logo + hero
                team.LogoUrl = ts.StrBadge ?? ts.StrLogo ?? team.LogoUrl;
                team.HeroImageUrl = ts.StrFanart1 ?? team.HeroImageUrl;
            }

            await _ctx.SaveChangesAsync();
            return upserts;
        }

        private static DateTime? ParseEventDateUtc(TsEvent e)
        {
            // 1) strTimestamp (souvent en UTC) si dispo
            if (!string.IsNullOrWhiteSpace(e.StrTimestamp) &&
                DateTime.TryParse(e.StrTimestamp, out var ts))
            {
                return DateTime.SpecifyKind(ts, DateTimeKind.Utc);
            }

            // 2) dateEvent + strTime
            if (!string.IsNullOrWhiteSpace(e.DateEvent))
            {
                var datePart = e.DateEvent;
                var timePart = string.IsNullOrWhiteSpace(e.StrTime) ? "20:00:00" : e.StrTime;

                if (DateTime.TryParse($"{datePart} {timePart}", out var local))
                {
                    // TheSportsDB ne précise pas toujours le fuseau → on suppose local et on passe en UTC
                    return DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime();
                }
            }

            return null;
        }

        private static int? ParseScore(string? score)
        {
            if (int.TryParse(score, out var s)) return s;
            return null;
        }

        /// <summary>
        /// S’assure qu’une équipe existe (par id externe / nom), sinon la crée.
        /// Utilisé dans la 1ère passe sur les events.
        /// </summary>
        private Team EnsureTeamFromEvent(
            League league,
            string? idTeam,
            string? name,
            Dictionary<int, Team> teamsByExt,
            Dictionary<string, Team> teamsByName,
            ref int createdCount)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("EnsureTeamFromEvent called with empty name");

            Team? team = null;
            int? extId = null;

            if (!string.IsNullOrWhiteSpace(idTeam) && int.TryParse(idTeam, out var parsedExt))
            {
                extId = parsedExt;
                if (teamsByExt.TryGetValue(parsedExt, out var byExt))
                    team = byExt;
            }

            if (team == null && teamsByName.TryGetValue(name, out var byName))
            {
                team = byName;
            }

            if (team != null)
            {
                // On peut mettre à jour l’ExternalId si on vient de le découvrir
                if (extId.HasValue && team.ExternalId != extId.Value)
                {
                    team.ExternalId = extId.Value;
                    teamsByExt[extId.Value] = team;
                }
                return team;
            }

            // Création d’une nouvelle équipe
            team = new Team
            {
                LeagueId = league.Id,
                Name = name,
                ExternalId = extId
            };
            _ctx.Teams.Add(team);
            createdCount++;

            if (extId.HasValue)
                teamsByExt[extId.Value] = team;

            teamsByName[name] = team;

            Console.WriteLine($"[EuroLeague] CREATED missing team from events: id={extId} name='{name}'");
            return team;
        }

        /// <summary>
        /// Upsert des matchs EuroLeague pour une saison donnée, ex "2025".
        /// </summary>
        public async Task<int> SyncSeasonGamesAsync(string season)
        {
            var league = await GetOrCreateEuroleagueAsync();

            // 1) Saison (tronquée mais utile pour le début de saison)
            var seasonEvents = await _api.GetEuroleagueEventsSeasonAsync(season);

            // 2) Derniers matchs de la ligue (résultats récents)
            var pastEvents = await _api.GetEuroleaguePastLeagueEventsAsync();

            // 3) Prochains matchs de la ligue
            var nextEvents = await _api.GetEuroleagueNextLeagueEventsAsync();

            // On fusionne et on vire les doublons par idEvent
            var events = seasonEvents
                .Concat(pastEvents)
                .Concat(nextEvents)
                .GroupBy(e => e.IdEvent)
                .Select(g => g.First())
                .ToList();

            Console.WriteLine($"[EuroLeague] Total events combinés = {events.Count}");

            // ─────────────────────────────────────────────
            // PASS 1 : s’assurer que toutes les équipes des events existent
            // ─────────────────────────────────────────────
            var teamsByExt = await _ctx.Teams
                .Where(t => t.LeagueId == league.Id && t.ExternalId != null)
                .ToDictionaryAsync(t => t.ExternalId!.Value);

            var teamsByName = (await _ctx.Teams
                    .Where(t => t.LeagueId == league.Id)
                    .ToListAsync())
                .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var createdTeamsFromEvents = 0;

            foreach (var ev in events)
            {
                if (string.IsNullOrWhiteSpace(ev.StrHomeTeam) ||
                    string.IsNullOrWhiteSpace(ev.StrAwayTeam))
                {
                    // on laissera le compteur skippedNoTeams pour la seconde passe
                    continue;
                }

                // Home
                EnsureTeamFromEvent(
                    league,
                    ev.IdHomeTeam,
                    ev.StrHomeTeam,
                    teamsByExt,
                    teamsByName,
                    ref createdTeamsFromEvents);

                // Away
                EnsureTeamFromEvent(
                    league,
                    ev.IdAwayTeam,
                    ev.StrAwayTeam,
                    teamsByExt,
                    teamsByName,
                    ref createdTeamsFromEvents);
            }

            if (createdTeamsFromEvents > 0)
            {
                await _ctx.SaveChangesAsync();
                Console.WriteLine($"[EuroLeague] Créé {createdTeamsFromEvents} équipes manquantes à partir des events.");
            }

            // ─────────────────────────────────────────────
            // Rechargement propre des équipes & jeux connus
            // ─────────────────────────────────────────────
            teamsByExt = await _ctx.Teams
                .Where(t => t.LeagueId == league.Id && t.ExternalId != null)
                .ToDictionaryAsync(t => t.ExternalId!.Value);

            teamsByName = (await _ctx.Teams
                    .Where(t => t.LeagueId == league.Id)
                    .ToListAsync())
                .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var gamesByExt = await _ctx.Games
                .Where(g => g.LeagueId == league.Id && g.ExternalId != null && g.Source == "thesportsdb")
                .ToDictionaryAsync(g => g.ExternalId!.Value);

            var upserts = 0;
            var skippedNoTeams = 0;
            var skippedNoMapping = 0;
            var skippedNoDate = 0;

            // ─────────────────────────────────────────────
            // PASS 2 : création / mise à jour des matchs
            // ─────────────────────────────────────────────
            foreach (var ev in events)
            {
                if (string.IsNullOrWhiteSpace(ev.StrHomeTeam) ||
                    string.IsNullOrWhiteSpace(ev.StrAwayTeam))
                {
                    skippedNoTeams++;
                    Console.WriteLine($"[EuroLeague] SKIP no teams: idEvent={ev.IdEvent} home='{ev.StrHomeTeam}' away='{ev.StrAwayTeam}'");
                    continue;
                }

                Team? home = null;
                Team? away = null;

                if (int.TryParse(ev.IdHomeTeam, out var homeExt) &&
                    teamsByExt.TryGetValue(homeExt, out var homeByExt))
                {
                    home = homeByExt;
                }
                else if (teamsByName.TryGetValue(ev.StrHomeTeam, out var homeByName))
                {
                    home = homeByName;
                }

                if (int.TryParse(ev.IdAwayTeam, out var awayExt) &&
                    teamsByExt.TryGetValue(awayExt, out var awayByExt))
                {
                    away = awayByExt;
                }
                else if (teamsByName.TryGetValue(ev.StrAwayTeam, out var awayByName))
                {
                    away = awayByName;
                }

                if (home == null || away == null)
                {
                    skippedNoMapping++;
                    Console.WriteLine($"[EuroLeague] SKIP mapping teams: idEvent={ev.IdEvent} home='{ev.StrHomeTeam}'({ev.IdHomeTeam}) away='{ev.StrAwayTeam}'({ev.IdAwayTeam})");
                    continue;
                }

                // Date / heure
                var parsedDate = ParseEventDateUtc(ev);

                if (parsedDate == null)
                {
                    skippedNoDate++;
                    Console.WriteLine($"[EuroLeague] WARN date null: idEvent={ev.IdEvent} dateEvent='{ev.DateEvent}' time='{ev.StrTime}' strTimestamp='{ev.StrTimestamp}'");
                    // on ne SKIP plus, on met une date par défaut pour ne pas perdre le match
                    parsedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                }

                var startAt = parsedDate.Value;

                int? extId = null;
                if (int.TryParse(ev.IdEvent, out var parsedId))
                    extId = parsedId;

                Game? game = null;
                if (extId.HasValue && gamesByExt.TryGetValue(extId.Value, out var gExisting))
                {
                    game = gExisting;
                }

                if (game == null)
                {
                    game = new Game
                    {
                        LeagueId = league.Id,
                        HomeTeamId = home.Id,
                        AwayTeamId = away.Id,
                        StartAt = startAt,
                        Venue = ev.StrVenue ?? "",
                        ExternalId = extId,
                        Source = "thesportsdb"
                    };
                    _ctx.Games.Add(game);
                    upserts++;
                }
                else
                {
                    game.HomeTeamId = home.Id;
                    game.AwayTeamId = away.Id;
                    game.StartAt = startAt;
                    game.Venue = ev.StrVenue ?? game.Venue;
                }

                // Saison numérique (prend l’année de début)
                if (!string.IsNullOrWhiteSpace(ev.StrSeason) &&
                    ev.StrSeason.Length >= 4 &&
                    int.TryParse(ev.StrSeason.Substring(0, 4), out var seasonYear))
                {
                    game.Season = seasonYear;
                }

                game.HomeScore = ParseScore(ev.IntHomeScore);
                game.AwayScore = ParseScore(ev.IntAwayScore);
                game.Status = ev.StrStatus;
            }

            await _ctx.SaveChangesAsync();

            Console.WriteLine(
                $"[EuroLeague] Fin SyncSeasonGamesAsync {season} -> upserts={upserts}, " +
                $"skippedNoTeams={skippedNoTeams}, skippedNoMapping={skippedNoMapping}, skippedNoDate={skippedNoDate}");

            return upserts;
        }

    }
}
