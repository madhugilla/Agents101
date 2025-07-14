// See https://aka.ms/new-console-template for more information
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var config = LoadConfiguration();
        var projectEndpoint = config["AzureAI:ProjectEndpoint"];
        var modelDeploymentName = config["AzureAI:ModelDeploymentName"];
        PersistentAgentsClient agentsClient = AzureAIAgent.CreateAgentsClient(projectEndpoint, new AzureCliCredential());

        // 1. Define an agent on the Azure AI agent service
        PersistentAgent definition = await agentsClient.Administration.CreateAgentAsync(
             modelDeploymentName,
            name: "<agent name>",
            description: "<agent description>",
            instructions: "<agent instructions>");

        // 2. Create a Semantic Kernel agent based on the agent definition
        AzureAIAgent agent = new(definition, agentsClient);

        AzureAIAgentThread agentThread = new(agent.Client);
        try
        {
            ChatMessageContent message = new(AuthorRole.User, "<your user input>");
            await foreach (ChatMessageContent response in agent.InvokeAsync(message, agentThread))
            {
                Console.WriteLine(response.Content);
            }
        }
        finally
        {
            await agentThread.DeleteAsync();
            await agent.Client.Administration.DeleteAgentAsync(agent.Id);
        }

    }

    private static IConfiguration LoadConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }

}
