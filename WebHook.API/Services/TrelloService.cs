using System.Net.Http.Json;
using System.Text.Json;

namespace WebHook.API.Services
{
    public class TrelloService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public TrelloService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public async Task<JsonElement?> GetCardAsync(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId)) return null;

            // Build Trello API URL. If API key/token present, include them.
            var key = _config["Trello:Key"];
            var token = _config["Trello:Token"];
            var url = $"https://api.trello.com/1/cards/{cardId}?fields=name,desc,shortUrl";
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(token))
            {
                url += $"&key={key}&token={token}";
            }

            try
            {
                var res = await _http.GetAsync(url);
                if (!res.IsSuccessStatusCode) return null;
                using var stream = await res.Content.ReadAsStreamAsync();
                var doc = await JsonDocument.ParseAsync(stream);
                return doc.RootElement;
            }
            catch
            {
                return null;
            }
        }
    }
}
