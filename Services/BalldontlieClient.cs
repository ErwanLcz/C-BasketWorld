using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace BasketWorld.Services
{
    public class BalldontlieClient
    {
        private readonly HttpClient _http;
        private readonly string _base;
        private readonly string? _key;

        public BalldontlieClient(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _base = cfg["Balldontlie:BaseUrl"] ?? "https://api.balldontlie.io/v1";
            _key  = cfg["Balldontlie:ApiKey"];
        }

        // ------- Core GET avec Retry/Backoff -------
        private async Task<T?> GetJsonWithRetryAsync<T>(string url, int maxRetries = 4, int baseDelayMs = 400)
        {
            int attempt = 0;
            while (true)
            {
                attempt++;
                var req = new HttpRequestMessage(HttpMethod.Get, url);
                if (!string.IsNullOrWhiteSpace(_key))
                    req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _key);

                req.Headers.UserAgent.ParseAdd("BasketWorld/1.0");

                var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);

                if (res.IsSuccessStatusCode)
                    return await res.Content.ReadFromJsonAsync<T>();

                // 429: respect Retry-After si présent
                if (res.StatusCode == HttpStatusCode.TooManyRequests && attempt <= maxRetries)
                {
                    var retryAfter = res.Headers.RetryAfter?.Delta ?? TimeSpan.FromMilliseconds(baseDelayMs * Math.Pow(2, attempt - 1));
                    await Task.Delay(retryAfter);
                    continue;
                }

                // 5xx: tenter backoff aussi
                if ((int)res.StatusCode >= 500 && attempt <= maxRetries)
                {
                    await Task.Delay((int)(baseDelayMs * Math.Pow(2, attempt - 1)));
                    continue;
                }

                // Sinon, lève une erreur plus explicite
                var content = await res.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"API balldontlie {res.StatusCode}: {content}");
            }
        }

        // ------- Teams -------
        public async Task<List<BlTeam>> GetTeamsAsync()
        {
            var all = new List<BlTeam>();
            var page = 1;
            while (true)
            {
                var url = $"{_base}/teams?per_page=100&page={page}";
                var res = await GetJsonWithRetryAsync<BlPaginated<BlTeam>>(url);
                if (res == null || res.Data.Count == 0) break;
                all.AddRange(res.Data);
                if (res.Meta.Next_page == 0) break;
                page++;
            }
            return all;
        }

        // ------- Games par fenêtre, avec dates batchées -------
        public async Task<List<BlGame>> GetGamesByDateRangeAsync(DateTime fromUtc, DateTime toUtc)
        {
            var all = new List<BlGame>();

            // balldontlie accepte start_date / end_date + seasons[]
            // Exemple: /games?start_date=2025-10-01&end_date=2025-10-31&seasons[]=2025&per_page=100&page=1
            var start = fromUtc.Date.ToString("yyyy-MM-dd");
            var end   = toUtc.Date.ToString("yyyy-MM-dd");

            // On met la saison du 'from' ET celle du 'to' (au cas où on chevauche)
            var seasons = new HashSet<int> { fromUtc.Year, toUtc.Year };

            foreach (var season in seasons)
            {
                var page = 1;
                while (true)
                {
                    var url = $"{_base}/games?start_date={start}&end_date={end}&seasons[]={season}&per_page=100&page={page}";
                    var res = await GetJsonWithRetryAsync<BlPaginated<BlGame>>(url);
                    if (res == null || res.Data.Count == 0) break;

                    all.AddRange(res.Data);

                    if (res.Meta.Next_page == 0) break;
                    page++;

                    await Task.Delay(200); // petite pause entre pages
                }
            }

            return all;
        }

    }
}
