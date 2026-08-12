namespace GastroLeinefeldeAPI;

public sealed class AppSettings
{
    public string DefaultUrl { get; set; } = "https://essen-auf-raedern-eichsfeld.de/tagesangebot";
    public int DeactivateOldAfterDays { get; set; } = 7;
}
