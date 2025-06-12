# Azure AI Agents 101

A C# console application demonstrating the use of Azure AI Agents Persistent API for different scenarios:

## Features

- **RunAgentDemo**: Basic agent demo that creates a math visualization agent
- **OCRToJsonAgent**: OCR agent that extracts text from images and converts it to JSON format

## Prerequisites

- .NET 9.0 or later
- Azure AI Services endpoint
- Appropriate Azure credentials configured

## Configuration

Update the following variables in the code with your Azure AI Services details:

- `projectEndpoint`: Your Azure AI Services project endpoint
- `modelDeploymentName`: The deployed model name (e.g., "gpt-4o-mini", "o3-mini")

## Usage

The application currently runs the `OCRToJsonAgent` method by default. You can modify the `Main` method to run different demos:

```csharp
static void Main(string[] args)
{
    // RunAgentDemo();
    OCRToJsonAgent();
}
```

## Dependencies

- Azure.AI.Agents.Persistent
- Azure.Identity
- System.Diagnostics

## Getting Started

1. Clone this repository
2. Update the configuration values
3. Build and run the application

```bash
dotnet build
dotnet run
```
