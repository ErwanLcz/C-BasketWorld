using System.Xml.Serialization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace BasketWorld.Services
{
    public class EuroleagueOfficialClient 
    {
        private readonly HttpClient _http;

        public EuroleagueOfficialClient(HttpClient http)
        {
            _http = http;
        }

        /// <summary>
        /// Récupère le calendrier pour une saison (schedule XML).
        /// seasonCode: ex "E2025" (à adapter en fonction de ce que demande l'API).
        /// </summary>
        public async Task<List<ScheduleItem>> GetSeasonScheduleAsync(string seasonCode)
        {
            // ✅ On colle EXACTEMENT à ton curl qui marche :
            // GET https://api-live.euroleague.net/v1/schedules?seasonCode=E2025
            var url = $"v1/schedules?seasonCode={seasonCode}";

            using var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"EuroLeague API error: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync();

            var serializer = new XmlSerializer(typeof(ScheduleResponse));
            var root = (ScheduleResponse?)serializer.Deserialize(stream);

            return root?.Items ?? new List<ScheduleItem>();
        }

        /// <summary>
        /// Récupère le détail d’un match (scores, etc.)
        /// via /v1/games?seasonCode=E2025&gameCode=7
        /// </summary>
        public async Task<GameResultDto?> GetGameResultAsync(string seasonCode, int gameCode)
        {
            // baseAddress déjà configurée sur https://api-live.euroleague.net/
            var url = $"v1/games?seasonCode={seasonCode}&gameCode={gameCode}";

            using var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[EuroLeagueOfficial] GetGameResultAsync {url} -> HTTP {(int)response.StatusCode}");
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync();

            var serializer = new XmlSerializer(typeof(EuroleagueGameDetails));
            if (serializer.Deserialize(stream) is not EuroleagueGameDetails xml)
            {
                return null;
            }

            var dto = new GameResultDto
            {
                SeasonCode   = xml.SeasonCode,
                GameCode     = xml.Code,
                HomeTeamCode = xml.LocalClub?.Code,
                AwayTeamCode = xml.RoadClub?.Code,
                HomeScore    = xml.LocalClub?.Score ?? 0,
                AwayScore    = xml.RoadClub?.Score ?? 0,
                Status       = xml.Played ? "Final" : "Scheduled"
            };

            return dto;
        }

    }
}
