using System.ComponentModel.DataAnnotations;

namespace GastroLeinefeldeAPI.Models;

public class Meal
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Category { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;
    
    public decimal? Price { get; set; }
    
    [MaxLength(50)]
    public string? Status { get; set; }
    
    [MaxLength(50)]
    public string? PreparationTime { get; set; }
    
    public DateTime? Date { get; set; }
    
    [MaxLength(20)]
    public string? DayOfWeek { get; set; }
    
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    
    public string? Source { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    [MaxLength(64)]
    public string? Hash { get; set; }
}