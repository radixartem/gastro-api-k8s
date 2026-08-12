using Microsoft.AspNetCore.Mvc;

namespace GastroLeinefeldeAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { 
            message = "API läuft!", 
            timestamp = DateTime.UtcNow,
            status = "online"
        });
    }
}