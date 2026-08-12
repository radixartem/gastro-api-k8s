namespace GastroLeinefeldeAPI.Services;

public interface IWebsiteClient
{
    Task<string> FetchHtmlAsync(string url);
}