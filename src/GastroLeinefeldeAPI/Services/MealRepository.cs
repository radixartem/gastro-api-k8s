using System.Security.Cryptography;
using System.Text;
using GastroLeinefeldeAPI.Data;
using GastroLeinefeldeAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GastroLeinefeldeAPI.Services;

public class MealRepository : IMealRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<MealRepository> _logger;

    public MealRepository(AppDbContext context, ILogger<MealRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Meal?> GetByIdAsync(int id) => await _context.Meals.FindAsync(id);

    public async Task<IEnumerable<Meal>> GetAllAsync() => await _context.Meals
        .OrderByDescending(m => m.ImportedAt).ToListAsync();

    public async Task<Meal> AddAsync(Meal meal)
    {
        meal.ImportedAt = DateTime.UtcNow;
        meal.Hash = ComputeHash(meal);
        await _context.Meals.AddAsync(meal);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Added meal {Name}", meal.Name);
        return meal;
    }

    public async Task<Meal> UpdateAsync(Meal meal)
    {
        meal.Hash = ComputeHash(meal);
        meal.ImportedAt = DateTime.UtcNow;
        _context.Meals.Update(meal);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated meal {Name}", meal.Name);
        return meal;
    }

    public async Task<Meal?> GetByHashAsync(string hash) => await _context.Meals
        .FirstOrDefaultAsync(m => m.Hash == hash);

    public async Task<Meal?> GetByIdentityAsync(string category, string name) => await _context.Meals
        .Where(m => m.Category == category && m.Name == name)
        .OrderByDescending(m => m.IsActive)
        .ThenByDescending(m => m.ImportedAt)
        .FirstOrDefaultAsync();

    public async Task TouchAsync(Meal meal, string source)
    {
        meal.ImportedAt = DateTime.UtcNow;
        meal.IsActive = true;
        meal.Source = source;
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Meal>> GetActiveMealsAsync() => await _context.Meals
        .Where(m => m.IsActive).OrderByDescending(m => m.ImportedAt).ToListAsync();

    public async Task<IEnumerable<Meal>> GetMealsByCategoryAsync(string category) => await _context.Meals
        .Where(m => m.Category == category && m.IsActive)
        .OrderByDescending(m => m.ImportedAt).ToListAsync();

    public Task<int> GetTotalCountAsync() => _context.Meals.CountAsync();

    public async Task<int> DeactivateOldMealsAsync(DateTime threshold)
    {
        var oldMeals = await _context.Meals
            .Where(m => m.IsActive && m.ImportedAt < threshold)
            .ToListAsync();

        foreach (var meal in oldMeals)
            meal.IsActive = false;

        if (oldMeals.Count > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deactivated {Count} old meals", oldMeals.Count);
        }

        return oldMeals.Count;
    }

    private static string ComputeHash(Meal meal)
    {
        var input = $"{meal.Category}|{meal.Name}|{meal.Price}|{meal.Status}|{meal.PreparationTime}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
    }
}
