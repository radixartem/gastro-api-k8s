using System.Net;

namespace GastroLeinefeldeAPI.Services;

public class WebsiteClient : IWebsiteClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebsiteClient> _logger;

    public WebsiteClient(HttpClient httpClient, ILogger<WebsiteClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<string> FetchHtmlAsync(string url)
    {
        try
        {
            _logger.LogInformation("Lade HTML von {Url}", url);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var html = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(html))
                throw new InvalidOperationException("Leerer HTML-Inhalt empfangen.");
                
            _logger.LogInformation("HTML geladen, Größe: {Size} bytes", html.Length);
            return html;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new Exception($"Seite nicht gefunden (404): {url}", ex);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.InternalServerError)
        {
            throw new Exception($"Server-Fehler (500): {url}", ex);
        }
        catch (TaskCanceledException)
        {
            throw new Exception($"Zeitüberschreitung beim Laden von {url}");
        }
        catch (Exception ex)
        {
            throw new Exception($"Fehler beim Laden der Seite: {ex.Message}", ex);
        }
    }
}