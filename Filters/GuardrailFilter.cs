using Microsoft.SemanticKernel;

namespace FraudCopilot.Filters;

public class GuardrailFilter : IAutoFunctionInvocationFilter
{
    private int _callCount;
    private const int MaxCalls = 15;

    public async Task OnAutoFunctionInvocationAsync(
        AutoFunctionInvocationContext context,
        Func<AutoFunctionInvocationContext, Task> next)
    {
        _callCount++;

        if (_callCount > MaxCalls)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(
                $"  Warning: automatic tool calls exceeded the limit ({MaxCalls}). Stopping further tool invocations for this turn.");
            Console.ResetColor();
            context.Terminate = true;
            return;
        }

        await next(context);
    }
}
