namespace Agents101;

using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using System.Diagnostics;
class Program
{
    static void Main(string[] args)
    {
        // RunAgentDemo();
        OCRToJsonAgent();
    }

    static void OCRToJsonAgent()
    {
        var projectEndpoint = "https://cua-resource.services.ai.azure.com/api/projects/cua";
        var modelDeploymentName = "gpt-4o-mini";

        //Create a PersistentAgentsClient and PersistentAgent.
        PersistentAgentsClient persistentAgentsClient = new(projectEndpoint, new DefaultAzureCredential());

        //Give PersistentAgent a tool to execute code using CodeInterpreterToolDefinition.
        PersistentAgent agent = persistentAgentsClient.Administration.CreateAgent(
            model: modelDeploymentName,
            name: "Image to JSON Agent",
            instructions: "extract the text from the image into a valid json, use a flat hierarchy. " +
                          "The output should be a valid JSON object with the text extracted from the image."
        );

        //Create a thread to establish a session between Agent and a User.
        PersistentAgentThread thread = persistentAgentsClient.Threads.CreateThread();

        var imgUrlParam = new MessageImageUriParam("https://ibb.co/QFy1WbFk"
                   );

        var urlBlock = new MessageInputImageUriBlock(imgUrlParam);
        var msgContent = new List<MessageInputContentBlock>
            {
                new MessageInputTextBlock("extract the text from the image into a valid json, use a flat hierarchy."),
                urlBlock        // swap to `fileBlock` if you uploaded the image
            };

        //Ask a question of the Agent.
        persistentAgentsClient.Messages.CreateMessage(
            thread.Id,
            MessageRole.User,msgContent);

        //Have Agent beging processing user's question with some additional instructions associated with the ThreadRun.
        ThreadRun run = persistentAgentsClient.Runs.CreateRun(
            thread.Id,
            agent.Id
            );

        //Poll for completion.
        do
        {
            Console.WriteLine($"Run Status: {run.Status}");
            Thread.Sleep(TimeSpan.FromMilliseconds(1000));
            run = persistentAgentsClient.Runs.GetRun(thread.Id, run.Id);
        }
        while (run.Status == RunStatus.Queued
            || run.Status == RunStatus.InProgress
            || run.Status == RunStatus.RequiresAction);
        Console.WriteLine($"Run Status: {run.Status}");

        //Get the messages in the PersistentAgentThread. Includes Agent (Assistant Role) and User (User Role) messages.
        Pageable<PersistentThreadMessage> messages = persistentAgentsClient.Messages.GetMessages(
            threadId: thread.Id,
            order: ListSortOrder.Ascending);

        //Display each message and open the image generated using CodeInterpreterToolDefinition.
        foreach (PersistentThreadMessage threadMessage in messages)
        {
            foreach (MessageContent content in threadMessage.ContentItems)
            {
                switch (content)
                {
                    case MessageTextContent textItem:
                        Console.WriteLine($"[{threadMessage.Role}]: {textItem.Text}");
                        break;
                }
            }
        }

        //Clean up test resources.
        persistentAgentsClient.Threads.DeleteThread(threadId: thread.Id);
        persistentAgentsClient.Administration.DeleteAgent(agentId: agent.Id);
        Console.ReadLine();
    }
    static void RunAgentDemo()
    {
        Console.WriteLine("Hello, World!");

        var projectEndpoint = "https://cua-resource.services.ai.azure.com/api/projects/cua";
        var modelDeploymentName = "o3-mini";

        //Create a PersistentAgentsClient and PersistentAgent.
        PersistentAgentsClient persistentAgentsClient = new(projectEndpoint, new DefaultAzureCredential());

        //Give PersistentAgent a tool to execute code using CodeInterpreterToolDefinition.
        PersistentAgent agent = persistentAgentsClient.Administration.CreateAgent(
            model: modelDeploymentName,
            name: "My Test Agent",
            instructions: "You politely help with math questions. Use the code interpreter tool when asked to visualize numbers.",
            tools: [new CodeInterpreterToolDefinition()]
        );

        //Create a thread to establish a session between Agent and a User.
        PersistentAgentThread thread = persistentAgentsClient.Threads.CreateThread();

        //Ask a question of the Agent.
        persistentAgentsClient.Messages.CreateMessage(
            thread.Id,
            MessageRole.User,
            "Hi, Agent! Draw a graph for a line with a slope of 4 and y-intercept of 9.");

        //Have Agent beging processing user's question with some additional instructions associated with the ThreadRun.
        ThreadRun run = persistentAgentsClient.Runs.CreateRun(
            thread.Id,
            agent.Id,
            additionalInstructions: "Please address the user as Jane Doe. The user has a premium account.");

        //Poll for completion.
        do
        {
            Console.WriteLine($"Run Status: {run.Status}");
            Thread.Sleep(TimeSpan.FromMilliseconds(1000));
            run = persistentAgentsClient.Runs.GetRun(thread.Id, run.Id);
        }
        while (run.Status == RunStatus.Queued
            || run.Status == RunStatus.InProgress
            || run.Status == RunStatus.RequiresAction);
        Console.WriteLine($"Run Status: {run.Status}");
        //Get the messages in the PersistentAgentThread. Includes Agent (Assistant Role) and User (User Role) messages.
        Pageable<PersistentThreadMessage> messages = persistentAgentsClient.Messages.GetMessages(
            threadId: thread.Id,
            order: ListSortOrder.Ascending);

        //Display each message and open the image generated using CodeInterpreterToolDefinition.
        foreach (PersistentThreadMessage threadMessage in messages)
        {
            foreach (MessageContent content in threadMessage.ContentItems)
            {
                switch (content)
                {
                    case MessageTextContent textItem:
                        Console.WriteLine($"[{threadMessage.Role}]: {textItem.Text}");
                        break;
                    case MessageImageFileContent imageFileContent:
                        Console.WriteLine($"[{threadMessage.Role}]: Image content file ID = {imageFileContent.FileId}");
                        BinaryData imageContent = persistentAgentsClient.Files.GetFileContent(imageFileContent.FileId);
                        string tempFilePath = Path.Combine(AppContext.BaseDirectory, $"{Guid.NewGuid()}.png");
                        File.WriteAllBytes(tempFilePath, imageContent.ToArray());
                        persistentAgentsClient.Files.DeleteFile(imageFileContent.FileId);
                        Console.WriteLine($"Image saved to {tempFilePath}. Opening image...");
                        // Open the image using the default application associated with .png files.
                        ProcessStartInfo psi = new()
                        {
                            FileName = tempFilePath,
                            UseShellExecute = true
                        };
                        Process.Start(psi);
                        break;
                }
            }
        }        //Clean up test resources.
        persistentAgentsClient.Threads.DeleteThread(threadId: thread.Id);
        persistentAgentsClient.Administration.DeleteAgent(agentId: agent.Id);
        Console.ReadLine();
    }


}
