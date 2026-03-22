namespace FraudCopilot.Models;

public class FraudPattern
{
    public string PatternId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Indicators { get; set; } = new();
}
