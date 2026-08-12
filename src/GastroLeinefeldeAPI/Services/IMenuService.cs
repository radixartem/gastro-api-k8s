using GastroLeinefeldeAPI.Models;

namespace GastroLeinefeldeAPI.Services;

public interface IMenuService
{
    Task<ImportResult> ImportMenuAsync(string url);
    Task<IEnumerable<MealDto>> GetAllMealsAsync();
    Task<MealDto?> GetMealByIdAsync(int id);
    Task<IEnumerable<MealDto>> GetActiveMealsAsync();
    Task<IEnumerable<MealDto>> GetMealsByCategoryAsync(string category);
}