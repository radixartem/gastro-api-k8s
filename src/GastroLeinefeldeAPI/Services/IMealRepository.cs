using GastroLeinefeldeAPI.Models;

namespace GastroLeinefeldeAPI.Services;

public interface IMealRepository
{
    Task<Meal?> GetByIdAsync(int id);
    Task<IEnumerable<Meal>> GetAllAsync();
    Task<Meal> AddAsync(Meal meal);
    Task<Meal> UpdateAsync(Meal meal);
    Task<Meal?> GetByHashAsync(string hash);
    Task<Meal?> GetByIdentityAsync(string category, string name);
    Task TouchAsync(Meal meal, string source);
    Task<IEnumerable<Meal>> GetActiveMealsAsync();
    Task<IEnumerable<Meal>> GetMealsByCategoryAsync(string category);
    Task<int> GetTotalCountAsync();
    Task<int> DeactivateOldMealsAsync(DateTime threshold);
}
