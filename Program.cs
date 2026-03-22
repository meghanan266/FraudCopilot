using FraudCopilot.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;

foreach (var line in File.ReadAllLines(".env"))
{
    var parts = line.Split('=', 2);
    if (parts.Length == 2)
        Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
}

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? string.Empty;
var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY") ?? string.Empty;
var deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ?? string.Empty;

var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddAzureOpenAIChatCompletion(
    deploymentName: deployment,
    endpoint: endpoint,
    apiKey: apiKey);

kernelBuilder.Plugins.AddFromType<TransactionSearchPlugin>();
kernelBuilder.Plugins.AddFromType<RiskScoringPlugin>();
kernelBuilder.Plugins.AddFromType<FraudReportPlugin>();

var kernel = kernelBuilder.Build();
var chat = kernel.GetRequiredService<IChatCompletionService>();

const string systemPrompt =
    """
    You are FraudCopilot, an AI-powered fraud investigation agent for a financial services platform.
    Your job is to investigate accounts for suspicious activity using your available tools and produce structured fraud investigation reports.

    When given an investigation task:
    1. First list available accounts to orient yourself
    2. Retrieve the account's full transaction history
    3. Score the account's risk using the risk scoring tool
    4. Match the account's behavior against known fraud patterns
    5. Produce a structured report with the following sections: Investigation Summary, Flagged Transactions, Risk Analysis, Pattern Matches, Recommended Action

    Always cite specific transaction IDs and amounts when flagging suspicious activity.
    Be thorough — do not stop after one tool call.
    Format your final report with clear headings and bullet points.
    """;

var history = new ChatHistory(systemPrompt);

var executionSettings = new OpenAIPromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
};

Console.WriteLine("FraudCopilot — AI Fraud Investigation Agent");
Console.WriteLine("Type an account to investigate or describe a task. Type 'exit' to quit.");

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();
    if (input is null)
    {
        continue;
    }

    if (string.Equals(input.Trim(), "exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    history.AddUserMessage(input);

    var result = await chat.GetChatMessageContentAsync(history, executionSettings, kernel);
    history.Add(result);

    Console.WriteLine();
    Console.WriteLine(result.Content);
    Console.WriteLine();
}
