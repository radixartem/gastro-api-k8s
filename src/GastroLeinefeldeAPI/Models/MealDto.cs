namespace GastroLeinefeldeAPI.Models;

public class MealDto
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public string? Status { get; set; }
    public string? PreparationTime { get; set; }
    public DateTime? Date { get; set; }
    public DateTime ImportedAt { get; set; }
    public string? Source { get; set; }
    public bool IsActive { get; set; }
}

public class ImportResult
{
    public int Total { get; set; }
    public int New { get; set; }
    public int Updated { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public string? Source { get; set; }
}