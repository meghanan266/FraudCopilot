# FraudCopilot

Console agent that investigates mock account activity using Semantic Kernel tools and produces structured fraud-style reports.

## Tech stack

- .NET 9  
- Microsoft.SemanticKernel 1.71.0  
- Azure OpenAI (chat completion)  
- C#

## Concepts demonstrated

- Autonomous ReAct-style reasoning loop (non-streaming chat with tool use)  
- Multi-tool orchestration (search, risk scoring, patterns, report helpers)  
- `IAutoFunctionInvocationFilter` for tool-call logging and invocation guardrails  
- Plugin-based architecture (`KernelFunction` methods on POCOs)  
- Structured fraud report generation via prompts and tools  

## Project structure

| Path | Purpose |
|------|--------|
| `Program.cs` | `.env` loading, Azure OpenAI kernel, plugins, filters, chat loop |
| `Models/` | `Transaction`, `Account`, `FraudPattern` |
| `Data/` | `MockDataStore` — sample accounts, transactions, patterns |
| `Plugins/` | `TransactionSearchPlugin`, `RiskScoringPlugin`, `FraudReportPlugin` |
| `Filters/` | `ToolLoggingFilter`, `GuardrailFilter` |

## Setup

1. Clone or copy the repository.  
2. Copy `.env` in the project root and set:
   - `AZURE_OPENAI_ENDPOINT` — resource endpoint URL  
   - `AZURE_OPENAI_KEY` — API key  
   - `AZURE_OPENAI_DEPLOYMENT` — chat deployment name  
3. From the project directory: `dotnet run`  

Ensure the shell working directory is the project folder so `.env` is found.

## Example tasks

- `Investigate account ACCT-7741-WS and give a full report.`  
- `List accounts, then analyze ACCT-8820-MK for velocity and cross-border risk.`  
- `Score risk and match fraud patterns for ACCT-9912-PL.`  
- `Which transactions exceed $1,000? Then tie them to accounts and patterns.`  

## Note

This is a **demo** using **fictional mock data** only. It is not connected to real banking systems and is not production fraud detection.
