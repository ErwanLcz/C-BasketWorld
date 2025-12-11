using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace BasketWorld.Services
{
    public class TheSportsDbClient
    {
        private readonly HttpClient _http;
        private readonly string _base;
        private readonly string _key;

        private const int EuroleagueId = 4546;
        private const string EuroleagueName = "EuroLeague Basketball";

        public class TsTeamList
        {
            // ⚠️ important : le JSON renvoie "teams" (minuscule)
            public List<TsTeam>? teams { get; set; }
        }

        public TheSportsDbClient(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _base = cfg["TheSportsDb:BaseUrl"] ?? "https://www.thesportsdb.com/api/v1/json";
            _key = cfg["TheSportsDb:ApiKey"] ?? "123";
        }

        private string BuildUrl(string endpointAndQuery)
        {
            // ex: endpointAndQuery = "search_all_teams.php?l=EuroLeague%20Basketball"
            return $"{_base}/{_key}/{endpointAndQuery}";
        }

        private async Task<T?> GetJsonWithRetryAsync<T>(string endpointAndQuery)
        {
            var url = BuildUrl(endpointAndQuery);

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    var res = await _http.GetAsync(url);
                    if (res.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        // Rate limit dépassé → petite pause
                        await Task.Delay(1000 * attempt);
                        continue;
                    }

                    res.EnsureSuccessStatusCode();
                    var data = await res.Content.ReadFromJsonAsync<T>();
                    return data;
                }
                catch when (attempt < 3)
                {
                    await Task.Delay(500 * attempt);
                }
            }

            return default;
        }

        /// <summary>
        /// Toutes les équipes de l’EuroLeague.
        /// </summary>
        public async Task<List<TsTeam>> GetEuroleagueTeamsAsync()
        {
            // Nouvelle route : on utilise l’ID de ligue
            var res = await GetJsonWithRetryAsync<TsTeamList>(
                "lookup_all_teams.php?id=4546"
            );

            return res?.teams ?? new List<TsTeam>();
        }


        /// <summary>
        /// Tous les matchs de l’EuroLeague pour une saison donnée ("2025-2026").
        /// </summary>
        public async Task<List<TsEvent>> GetEuroleagueEventsSeasonAsync(string season)
        {
            var res = await GetJsonWithRetryAsync<TsEventsList>(
                $"eventsseason.php?id={EuroleagueId}&s={Uri.EscapeDataString(season)}"
            );

            var count = res?.events?.Count ?? 0;
            Console.WriteLine($"[EuroLeague] TheSportsDB a renvoyé {count} events pour la saison {season}");

            return res?.events ?? new List<TsEvent>();
        }
        public async Task<List<TsEvent>> GetEuroleagueNextLeagueEventsAsync()
        {
            // Prochains matchs EuroLeague
            var res = await GetJsonWithRetryAsync<TsEventsList>(
                $"eventsnextleague.php?id={EuroleagueId}"
            );

            return res?.events ?? new List<TsEvent>();
        }

        public async Task<List<TsEvent>> GetEuroleaguePastLeagueEventsAsync()
        {
            // Derniers matchs EuroLeague
            var res = await GetJsonWithRetryAsync<TsEventsList>(
                $"eventspastleague.php?id={EuroleagueId}"
            );

            return res?.events ?? new List<TsEvent>();
        }


    }
}
