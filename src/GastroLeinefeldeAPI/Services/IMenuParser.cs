using GastroLeinefeldeAPI.Models;

namespace GastroLeinefeldeAPI.Services;

public interface IMenuParser
{
    Task<IEnumerable<Meal>> ParseMenuAsync(string htmlContent);
}