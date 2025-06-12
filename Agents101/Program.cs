namespace Agents101;

using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using System.Diagnostics;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // RunAgentDemo();
        //OCRToJsonAgent();
        await OCRToJsonAgentWithFileUploadAsync();
    }

    static async Task OCRToJsonAgentWithFileUploadAsync()
    {
        var projectEndpoint = "https://cua-resource.services.ai.azure.com/api/projects/cua";
        var modelDeploymentName = "gpt-4o-mini";

        //Create a PersistentAgentsClient and PersistentAgent.
        PersistentAgentsClient persistentAgentsClient = new(projectEndpoint, new DefaultAzureCredential());

        await using var fs = File.OpenRead(@"images/parking.jpg");
        var imgInfo = await persistentAgentsClient.Files.UploadFileAsync(
            data: fs,
            purpose: PersistentAgentFilePurpose.Agents,
            filename: "parking.jpg");

        //Give PersistentAgent a tool to execute code using CodeInterpreterToolDefinition.
        PersistentAgent agent = persistentAgentsClient.Administration.CreateAgent(
            model: modelDeploymentName,
            name: "Image to JSON Agent",
            instructions: "extract the text from the image into a valid json, use a flat hierarchy. " +
                          "The output should be a valid JSON object with the text extracted from the image."
        );

        //Create a thread to establish a session between Agent and a User.
        PersistentAgentThread thread = persistentAgentsClient.Threads.CreateThread();


        var msgContent = new List<MessageInputContentBlock>
{
    new MessageInputTextBlock("extract the text from the image into a valid json, use a flat hierarchy. "),
    new MessageInputImageFileBlock(new MessageImageFileParam(imgInfo.Value.Id))
};

        //Ask a question of the Agent.
        persistentAgentsClient.Messages.CreateMessage(
            thread.Id,
            MessageRole.User, msgContent);

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

        var imgUrlParam = new MessageImageUriParam(
                  "data:image/jpeg;base64,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"
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
            MessageRole.User, msgContent);

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
