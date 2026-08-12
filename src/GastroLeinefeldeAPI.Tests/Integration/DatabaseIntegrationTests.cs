using GastroLeinefeldeAPI.Data;
using GastroLeinefeldeAPI.Models;
using GastroLeinefeldeAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace GastroLeinefeldeAPI.Tests.Integration;

public class DatabaseIntegrationTests : IDisposable
{
    private readonly AppDbContext context;
    private readonly MealRepository repository;

    public DatabaseIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        context = new AppDbContext(options);
        repository = new MealRepository(context, Microsoft.Extensions.Logging.Abstractions.NullLogger<MealRepository>.Instance);
    }

    [Fact]
    public async Task AddAndReadMeal()
    {
        var added = await repository.AddAsync(new Meal { Name = "Test", Category = "A", Price = 9.99m });
        var loaded = await repository.GetByIdAsync(added.Id);
        Assert.NotNull(loaded);
        Assert.Equal("Test", loaded!.Name);
    }

    [Fact]
    public async Task DeactivateOldMeals_ReturnsCount()
    {
        var old = new Meal { Name = "Old", Category = "A", ImportedAt = DateTime.UtcNow.AddDays(-10), IsActive = true };
        await repository.AddAsync(old);
        // AddAsync refreshes ImportedAt, so explicitly make it old afterwards.
        old.ImportedAt = DateTime.UtcNow.AddDays(-10);
        await context.SaveChangesAsync();
        var count = await repository.DeactivateOldMealsAsync(DateTime.UtcNow.AddDays(-7));
        Assert.Equal(1, count);
        Assert.False(old.IsActive);
    }

    public void Dispose()
    {
        context.Database.EnsureDeleted();
        context.Dispose();
    }
}
