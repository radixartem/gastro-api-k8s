using System.Text.Json;

namespace GastroLeinefeldeMenuParser.Services;

public class ApiDetector
{
    private readonly HttpClient _httpClient;

    public ApiDetector(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiInfo?> DetectApiAsync(string baseUrl)
    {
        var urlsToTry = new[]
        {
            "/api/menu",
            "/api/meals",
            "/api/daily",
            "/api/tagesangebot",
            "/data/menu.json",
            "/menu.json",
            "/api/offers",
            "/offers.json"
        };

        foreach (var endpoint in urlsToTry)
        {
            var fullUrl = $"{baseUrl.TrimEnd('/')}{endpoint}";
            try
            {
                var response = await _httpClient.GetAsync(fullUrl);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (IsValidJson(content))
                    {
                        return new ApiInfo
                        {
                            Url = fullUrl,
                            Type = "JSON API",
                            Sample = content.Length > 200 ? content.Substring(0, 200) + "..." : content
                        };
                    }
                }
            }
            catch
            {
                // Ignorieren und weitermachen
            }
        }

        // Prüfen auf JSON-LD
        try
        {
            var response = await _httpClient.GetAsync(baseUrl);
            if (response.IsSuccessStatusCode)
            {
                var html = await response.Content.ReadAsStringAsync();
                if (html.Contains("\"@type\"") || html.Contains("application/ld+json"))
                {
                    return new ApiInfo
                    {
                        Url = baseUrl,
                        Type = "JSON-LD (Structured Data)",
                        Sample = "JSON-LD im HTML gefunden"
                    };
                }
            }
        }
        catch { /* Ignorieren */ }

        return null;
    }

    private bool IsValidJson(string content)
    {
        try
        {
            JsonDocument.Parse(content);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public class ApiInfo
{
    public string Url { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Sample { get; set; } = string.Empty;
}