using GastroLeinefeldeAPI.Models;
using System.Security.Cryptography;
using System.Text;
using GastroLeinefeldeAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace GastroLeinefeldeAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MenuController : ControllerBase
{
    private const string DefaultUrl = "https://essen-auf-raedern-eichsfeld.de/tagesangebot";
    private readonly IMenuService _menuService;
    private readonly ILogger<MenuController> _logger;
    private readonly IConfiguration _configuration;

    public MenuController(IMenuService menuService, ILogger<MenuController> logger, IConfiguration configuration)
    {
        _menuService = menuService;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost("import")]
    public async Task<IActionResult> ImportMenu([FromQuery] string? url = null, [FromHeader(Name = "X-Import-Key")] string? importKey = null)
    {
        var configuredKey = _configuration["ImportApiKey"];
        if (string.IsNullOrWhiteSpace(configuredKey) || string.IsNullOrWhiteSpace(importKey) ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(configuredKey), Encoding.UTF8.GetBytes(importKey)))
        {
            return Unauthorized();
        }

        var targetUrl = string.IsNullOrWhiteSpace(url) ? DefaultUrl : url;
        if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return BadRequest(new { error = "Only absolute HTTPS URLs are allowed." });
        }

        try
        {
            return Ok(await _menuService.ImportMenuAsync(uri.ToString()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Menu import request failed 123");
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Menu import failed");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MealDto>>> GetAllMeals() => Ok(await _menuService.GetAllMealsAsync());

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<MealDto>>> GetActiveMeals() => Ok(await _menuService.GetActiveMealsAsync());

    [HttpGet("category/{category}")]
    public async Task<ActionResult<IEnumerable<MealDto>>> GetMealsByCategory(string category) => Ok(await _menuService.GetMealsByCategoryAsync(category));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetMealById(int id)
    {
        var meal = await _menuService.GetMealByIdAsync(id);
        return meal is null ? NotFound() : Ok(meal);
    }
}
