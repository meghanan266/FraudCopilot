using System.Text;
using Microsoft.SemanticKernel;

namespace FraudCopilot.Filters;

public class ToolLoggingFilter : IAutoFunctionInvocationFilter
{
    public async Task OnAutoFunctionInvocationAsync(
        AutoFunctionInvocationContext context,
        Func<AutoFunctionInvocationContext, Task> next)
    {
        var fn = context.Function.Name;
        var argText = FormatArguments(context.Arguments ?? new KernelArguments());

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"  → Calling: {fn}({argText})");
        Console.ResetColor();

        await next(context);

        var preview = BuildResultPreview(context);
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine($"  ← Result: {preview}");
        Console.ResetColor();
    }

    private static string FormatArguments(KernelArguments arguments)
    {
        if (arguments.Count == 0)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        foreach (var key in arguments.Keys)
        {
            var value = arguments[key];
            var literal = value?.ToString() ?? string.Empty;
            literal = literal.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
            parts.Add($"{key}=\"{literal}\"");
        }

        return string.Join(", ", parts);
    }

    private static string BuildResultPreview(AutoFunctionInvocationContext context)
    {
        string raw;
        if (context.Result is null)
        {
            raw = "(no result)";
        }
        else
        {
            raw = context.Result.ToString() ?? "(no result)";
        }

        const int maxLen = 120;
        if (raw.Length <= maxLen)
        {
            return raw;
        }

        return string.Concat(raw.AsSpan(0, maxLen), "...");
    }
}
