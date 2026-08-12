using System.Globalization;
using System.Text.RegularExpressions;
using GastroLeinefeldeAPI.Models;
using HtmlAgilityPack;

namespace GastroLeinefeldeAPI.Services;

public class MenuParser : IMenuParser
{
    private readonly ILogger<MenuParser> _logger;

    public MenuParser(ILogger<MenuParser> logger)
    {
        _logger = logger;
    }

    public Task<IEnumerable<Meal>> ParseMenuAsync(string htmlContent)
    {
        var meals = new List<Meal>();
        var doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        // Alle Textknoten finden
        var textNodes = doc.DocumentNode.SelectNodes("//text()");
        if (textNodes == null)
            return Task.FromResult<IEnumerable<Meal>>(meals);

        // Text sammeln und leere Zeilen filtern
        var lines = textNodes
            .Select(n => n.InnerText.Trim())
            .Where(t => !string.IsNullOrEmpty(t) && !t.StartsWith("++"))
            .ToList();

        string currentCategory = "";
        var currentMeal = new List<string>();

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            // Kategorien erkennen
            if (line.Contains("Angebot des Tages"))
            {
                if (currentMeal.Any())
                {
                    var meal = ParseMealFromLines(currentMeal, currentCategory);
                    if (meal != null) meals.Add(meal);
                    currentMeal.Clear();
                }
                currentCategory = "Angebot des Tages";
                continue;
            }
            else if (line.Contains("Unsere Klassiker"))
            {
                if (currentMeal.Any())
                {
                    var meal = ParseMealFromLines(currentMeal, currentCategory);
                    if (meal != null) meals.Add(meal);
                    currentMeal.Clear();
                }
                currentCategory = "Unsere Klassiker";
                continue;
            }
            else if (line.Contains("Die nächsten Tage") || line.Contains("Angebote vom"))
            {
                break;
            }

            // In einer Kategorie
            if (!string.IsNullOrEmpty(currentCategory))
            {
                // Preis (nur Zahl mit Komma/Punkt)
                if (IsPrice(line))
                {
                    currentMeal.Add(line);
                    if (currentMeal.Count >= 2)
                    {
                        var meal = ParseMealFromLines(currentMeal, currentCategory);
                        if (meal != null) meals.Add(meal);
                        currentMeal.Clear();
                    }
                    continue;
                }

                // Zeit (X Minuten)
                if (IsPreparationTime(line))
                {
                    currentMeal.Add(line);
                    continue;
                }

                // Name (alles andere)
                if (!IsDateRange(line) && !line.Contains("Angebote vom"))
                {
                    if (currentMeal.Any() && IsPrice(currentMeal.Last()))
                    {
                        var meal = ParseMealFromLines(currentMeal, currentCategory);
                        if (meal != null) meals.Add(meal);
                        currentMeal.Clear();
                    }
                    currentMeal.Add(line);
                }
            }
        }

        // Letztes Gericht
        if (currentMeal.Any())
        {
            var meal = ParseMealFromLines(currentMeal, currentCategory);
            if (meal != null) meals.Add(meal);
        }

        _logger.LogInformation("Parsing abgeschlossen. {Count} Gerichte gefunden.", meals.Count);
        return Task.FromResult<IEnumerable<Meal>>(meals);
    }

    private bool IsPrice(string text) => Regex.IsMatch(text, @"^(\d+[.,]\d{2})\s*[€]?$");
    private bool IsPreparationTime(string text) => Regex.IsMatch(text, @"^(\d+)\s*(Minuten?|min)$");
    private bool IsDateRange(string text) => Regex.IsMatch(text, @"\d{2}\.\d{2}\.\d{4}");

    private Meal? ParseMealFromLines(List<string> lines, string category)
    {
        var fullText = string.Join(" ", lines);
        fullText = Regex.Replace(fullText, @"\s+", " ").Trim();

        var meal = new Meal
        {
            Category = category,
            Name = string.Empty,
            Status = null,
            Price = null,
            PreparationTime = null,
            Date = DateTime.UtcNow,
            IsActive = true,
            ImportedAt = DateTime.UtcNow
        };

        // 1. Status in Sternchen (*ANGEBOT*, *AUS*, etc.)
        var statusMatch = Regex.Match(fullText, @"\*(?<status>[A-ZÄÖÜ]+)\*");
        if (statusMatch.Success)
        {
            meal.Status = statusMatch.Groups["status"].Value switch
            {
                "ANGEBOT" => "Angebot",
                "KIDSMENÜ" => "Kidsmenü",
                "AUS" => "Ausverkauft",
                _ => statusMatch.Groups["status"].Value
            };
            fullText = fullText.Replace(statusMatch.Value, "").Trim();
        }

        // 2. Preis
        var priceMatch = Regex.Match(fullText, @"(\d+[.,]\d{2})\s*[€]?");
        if (priceMatch.Success)
        {
            var priceStr = priceMatch.Groups[1].Value.Replace('.', ',');
            if (decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.GetCultureInfo("de-DE"), out var price))
                meal.Price = price;
            fullText = fullText.Replace(priceMatch.Value, "").Trim();
        }

        // 3. Zubereitungszeit
        var timeMatch = Regex.Match(fullText, @"(\d+)\s*(Minuten?|min)");
        if (timeMatch.Success)
        {
            meal.PreparationTime = timeMatch.Value.Trim();
            fullText = fullText.Replace(timeMatch.Value, "").Trim();
        }

        // 4. Aufräumen
        fullText = Regex.Replace(fullText, @"[\[\]\(\)\*]", " ");
        fullText = Regex.Replace(fullText, @"\s+", " ").Trim();

        // 5. Name speichern
        if (!string.IsNullOrEmpty(fullText) &&
            !Regex.IsMatch(fullText, @"^\d+[.,]\d+$") &&
            !fullText.Contains("Angebote vom") &&
            fullText.Length > 2)
        {
            meal.Name = fullText;
            meal.Hash = ComputeHash(meal);
            return meal;
        }

        return null;
    }

    private string ComputeHash(Meal meal)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var input = $"{meal.Category}|{meal.Name}|{meal.Price}|{meal.Status}|{meal.PreparationTime}";
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}