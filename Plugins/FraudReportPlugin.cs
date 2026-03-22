using System.ComponentModel;
using System.Globalization;
using System.Text;
using Microsoft.SemanticKernel;

namespace FraudCopilot.Plugins;

public class FraudReportPlugin
{
    [KernelFunction("create_report_section")]
    [Description("Create a formatted section for a fraud investigation report with a heading and findings")]
    public string CreateReportSection(
        [Description("Section heading")] string heading,
        [Description("Comma-separated list of findings")] string findings)
    {
        var sb = new StringBuilder();
        var title = string.IsNullOrWhiteSpace(heading) ? "Untitled section" : heading.Trim();
        sb.AppendLine($"## {title}");
        sb.AppendLine();

        if (string.IsNullOrWhiteSpace(findings))
        {
            sb.AppendLine("- (No findings provided.)");
            return sb.ToString().TrimEnd();
        }

        foreach (var raw in findings.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (raw.Length == 0)
            {
                continue;
            }

            sb.AppendLine($"- {raw}");
        }

        return sb.ToString().TrimEnd();
    }

    [KernelFunction("get_report_timestamp")]
    [Description("Get the current UTC timestamp for report dating")]
    public string GetReportTimestamp()
    {
        return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " UTC";
    }
}
