using System.Diagnostics;
using GastroLeinefeldeAPI.Models;
using Prometheus;

namespace GastroLeinefeldeAPI.Services;

public class MenuService : IMenuService
{
    private readonly IMealRepository _repository;
    private readonly IWebsiteClient _websiteClient;
    private readonly IMenuParser _parser;
    private readonly ILogger<MenuService> _logger;

    private static readonly Counter ImportedMealsTotal = Metrics.CreateCounter(
        "gastro_imported_meals_total",
        "Total number of meals processed during imports",
        new CounterConfiguration { LabelNames = ["type"] });

    private static readonly Counter ImportErrorsTotal = Metrics.CreateCounter(
        "gastro_import_errors_total",
        "Total number of import errors");

    private static readonly Counter ImportRequestsTotal = Metrics.CreateCounter(
        "gastro_import_requests_total",
        "Total number of completed import operations",
        new CounterConfiguration { LabelNames = ["status"] });

    private static readonly Gauge LastImportTimestamp = Metrics.CreateGauge(
        "gastro_last_import_timestamp",
        "Unix timestamp of the last successful import");

    private static readonly Histogram ImportDuration = Metrics.CreateHistogram(
        "gastro_import_duration_seconds",
        "Import duration in seconds");

    public MenuService(IMealRepository repository, IWebsiteClient websiteClient, IMenuParser parser, ILogger<MenuService> logger)
    {
        _repository = repository;
        _websiteClient = websiteClient;
        _parser = parser;
        _logger = logger;
    }

    public async Task<ImportResult> ImportMenuAsync(string url)
    {
        var result = new ImportResult { Source = url, Timestamp = DateTime.UtcNow };
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting menu import from {Url}", url);
            var html = await _websiteClient.FetchHtmlAsync(url);
            var mealList = (await _parser.ParseMenuAsync(html)).ToList();
            result.Total = mealList.Count;

            foreach (var meal in mealList)
            {
                if (string.IsNullOrWhiteSpace(meal.Name))
                    continue;

                var existing = await _repository.GetByHashAsync(meal.Hash ?? string.Empty);
                if (existing is not null)
                {
                    await _repository.TouchAsync(existing, url);
                    continue;
                }

                existing = await _repository.GetByIdentityAsync(meal.Category, meal.Name);
                if (existing is null)
                {
                    meal.Source = url;
                    meal.IsActive = true;
                    await _repository.AddAsync(meal);
                    result.New++;
                    ImportedMealsTotal.WithLabels("new").Inc();
                    continue;
                }

                if (HasMealChanged(existing, meal) || !existing.IsActive)
                {
                    existing.Price = meal.Price;
                    existing.Status = meal.Status;
                    existing.PreparationTime = meal.PreparationTime;
                    existing.Date = meal.Date;
                    existing.Source = url;
                    existing.IsActive = true;
                    await _repository.UpdateAsync(existing);
                    result.Updated++;
                    ImportedMealsTotal.WithLabels("updated").Inc();
                }
            }

            var deactivateAfterDays = 7;
            var deactivated = await _repository.DeactivateOldMealsAsync(DateTime.UtcNow.AddDays(-deactivateAfterDays));
            if (deactivated > 0)
                ImportedMealsTotal.WithLabels("deactivated").Inc(deactivated);

            LastImportTimestamp.Set(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            ImportRequestsTotal.WithLabels("success").Inc();

            _logger.LogInformation("Import completed: {New} new, {Updated} updated, {Deactivated} deactivated", result.New, result.Updated, deactivated);
            return result;
        }
        catch (Exception ex)
        {
            ImportErrorsTotal.Inc();
            ImportRequestsTotal.WithLabels("error").Inc();
            result.Errors.Add(ex.Message);
            _logger.LogError(ex, "Menu import failed for {Url}", url);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            ImportDuration.Observe(stopwatch.Elapsed.TotalSeconds);
        }
    }

    public async Task<IEnumerable<MealDto>> GetAllMealsAsync() => (await _repository.GetAllAsync()).Select(MapToDto);
    public async Task<MealDto?> GetMealByIdAsync(int id) => (await _repository.GetByIdAsync(id)) is { } meal ? MapToDto(meal) : null;
    public async Task<IEnumerable<MealDto>> GetActiveMealsAsync() => (await _repository.GetActiveMealsAsync()).Select(MapToDto);
    public async Task<IEnumerable<MealDto>> GetMealsByCategoryAsync(string category) => (await _repository.GetMealsByCategoryAsync(category)).Select(MapToDto);

    private static bool HasMealChanged(Meal existing, Meal newMeal) =>
        existing.Name != newMeal.Name || existing.Category != newMeal.Category || existing.Price != newMeal.Price ||
        existing.Status != newMeal.Status || existing.PreparationTime != newMeal.PreparationTime;

    private static MealDto MapToDto(Meal meal) => new()
    {
        Id = meal.Id,
        Category = meal.Category,
        Name = meal.Name,
        Price = meal.Price,
        Status = meal.Status,
        PreparationTime = meal.PreparationTime,
        Date = meal.Date,
        ImportedAt = meal.ImportedAt,
        Source = meal.Source,
        IsActive = meal.IsActive
    };
}
