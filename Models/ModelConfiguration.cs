namespace LocalizationTabii.Models;


public class ModelConfiguration
{
    public required string Identifier { get; init; }
    public required string Provider { get; init; }
    public required string ModelId { get; init; }
    public string? DisplayName { get; init; }
} 