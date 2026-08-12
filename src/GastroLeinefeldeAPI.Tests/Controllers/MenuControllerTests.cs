using GastroLeinefeldeAPI.Controllers;
using GastroLeinefeldeAPI.Models;
using GastroLeinefeldeAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;

namespace GastroLeinefeldeAPI.Tests.Controllers;

public class MenuControllerTests
{
    private readonly Mock<IMenuService> service = new();
    private readonly MenuController controller;

    public MenuControllerTests() => controller = new(service.Object, Mock.Of<ILogger<MenuController>>(), new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["ImportApiKey"] = "test-key" }).Build());

    [Fact]
    public async Task GetAllMeals_ReturnsOk()
    {
        service.Setup(x => x.GetAllMealsAsync()).ReturnsAsync(new[] { new MealDto { Id = 1, Name = "Schnitzel" } });
        var result = await controller.GetAllMeals();
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Single(Assert.IsAssignableFrom<IEnumerable<MealDto>>(ok.Value));
    }

    [Fact]
    public async Task GetMealById_ReturnsNotFoundWhenMissing()
    {
        service.Setup(x => x.GetMealByIdAsync(999)).ReturnsAsync((MealDto?)null);
        var result = await controller.GetMealById(999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ImportMenu_ReturnsOk()
    {
        service.Setup(x => x.ImportMenuAsync(It.IsAny<string>())).ReturnsAsync(new ImportResult { Total = 1, New = 1 });
        var result = await controller.ImportMenu(importKey: "test-key");
        Assert.IsType<OkObjectResult>(result);
    }
}
