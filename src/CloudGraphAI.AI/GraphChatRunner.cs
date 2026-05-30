using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace CloudGraphAI.AI;

public interface IGraphChatRunner
{
    Task RunAsync(Kernel kernel, CancellationToken cancellationToken = default);
}

public sealed class GraphChatRunner(IConfiguration configuration) : IGraphChatRunner
{
    public async Task RunAsync(Kernel kernel, CancellationToken cancellationToken = default)
    {
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        history.AddSystemMessage(configuration["AI:SystemPrompt"] ?? DefaultSystemPrompt);

        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write("Q: ");
            var question = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(question) || question.Equals("exit", StringComparison.OrdinalIgnoreCase))
                return;

            if (question.Equals("clear", StringComparison.OrdinalIgnoreCase))
            {
                history.Clear();
                history.AddSystemMessage(configuration["AI:SystemPrompt"] ?? DefaultSystemPrompt);
                continue;
            }

            history.AddUserMessage(question);
            var settings = new PromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            var answer = new StringBuilder();
            Console.WriteLine("A:");
            await foreach (var content in chat.GetStreamingChatMessageContentsAsync(history, settings, kernel, cancellationToken))
            {
                answer.Append(content.Content);
                Console.Write(content.Content);
            }

            Console.WriteLine();
            history.AddAssistantMessage(answer.ToString());
        }
    }

    private const string DefaultSystemPrompt = """
        You answer questions about cloud resources stored in Neo4j.
        Inspect the graph schema when needed, write read-only Cypher, execute it with the Neo4j tool, then answer from the returned rows.
        Use Cypher syntax, not SQL. Cypher does not support GROUP BY; aggregate by returning grouping keys alongside aggregate expressions, such as RETURN n.name AS name, sum(c.cost) AS totalCost.
        If a tool returns a cypher_query_failed error, revise the Cypher and call the tool again before answering.
        Do not guess when the graph does not contain enough data. Do not attempt writes or database administration commands.
        Prefer concise answers that mention the resource names and the relationship path that supports the answer.
        """;
}
