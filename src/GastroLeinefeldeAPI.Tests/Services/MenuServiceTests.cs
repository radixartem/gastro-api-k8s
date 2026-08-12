using GastroLeinefeldeAPI.Models;
using GastroLeinefeldeAPI.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace GastroLeinefeldeAPI.Tests.Services;

public class MenuServiceTests
{
    private readonly Mock<IMealRepository> repository = new();
    private readonly Mock<IWebsiteClient> website = new();
    private readonly Mock<IMenuParser> parser = new();
    private readonly MenuService service;

    public MenuServiceTests() => service = new(repository.Object, website.Object, parser.Object, Mock.Of<ILogger<MenuService>>());

    [Fact]
    public async Task ImportMenuAsync_AddsNewMeals()
    {
        website.Setup(x => x.FetchHtmlAsync(It.IsAny<string>())).ReturnsAsync("<html/>");
        parser.Setup(x => x.ParseMenuAsync(It.IsAny<string>())).ReturnsAsync(new[]
        {
            new Meal { Category = "Angebot des Tages", Name = "Schnitzel", Price = 9.90m, Hash = "h1" }
        });
        repository.Setup(x => x.GetByHashAsync("h1")).ReturnsAsync((Meal?)null);
        repository.Setup(x => x.GetByIdentityAsync("Angebot des Tages", "Schnitzel")).ReturnsAsync((Meal?)null);
        repository.Setup(x => x.AddAsync(It.IsAny<Meal>())).ReturnsAsync((Meal m) => m);
        repository.Setup(x => x.TouchAsync(It.IsAny<Meal>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        repository.Setup(x => x.DeactivateOldMealsAsync(It.IsAny<DateTime>())).ReturnsAsync(0);

        var result = await service.ImportMenuAsync("https://example.com");

        Assert.Equal(1, result.Total);
        Assert.Equal(1, result.New);
        repository.Verify(x => x.AddAsync(It.IsAny<Meal>()), Times.Once);
    }

    [Fact]
    public async Task ImportMenuAsync_UpdatesSameMealWhenPriceChanges()
    {
        var existing = new Meal { Id = 1, Category = "Angebot des Tages", Name = "Schnitzel", Price = 9.90m, Hash = "old" };
        var incoming = new Meal { Category = existing.Category, Name = existing.Name, Price = 10.90m, Hash = "new" };
        website.Setup(x => x.FetchHtmlAsync(It.IsAny<string>())).ReturnsAsync("<html/>");
        parser.Setup(x => x.ParseMenuAsync(It.IsAny<string>())).ReturnsAsync(new[] { incoming });
        repository.Setup(x => x.GetByHashAsync("new")).ReturnsAsync((Meal?)null);
        repository.Setup(x => x.GetByIdentityAsync(existing.Category, existing.Name)).ReturnsAsync(existing);
        repository.Setup(x => x.UpdateAsync(It.IsAny<Meal>())).ReturnsAsync((Meal m) => m);
        repository.Setup(x => x.TouchAsync(It.IsAny<Meal>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        repository.Setup(x => x.DeactivateOldMealsAsync(It.IsAny<DateTime>())).ReturnsAsync(0);

        var result = await service.ImportMenuAsync("https://example.com");

        Assert.Equal(1, result.Updated);
        Assert.Equal(10.90m, existing.Price);
        repository.Verify(x => x.UpdateAsync(existing), Times.Once);
    }
}
