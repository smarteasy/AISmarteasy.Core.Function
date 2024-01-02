using System.Runtime.CompilerServices;
using AISmarteasy.Core.Prompt;
using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Function;

public class SemanticFunction(string pluginName, string name, string description, IPromptTemplate promptTemplate, ILogger logger)
    : PluginFunction(pluginName, name, description, true, new List<ParameterInfo>())
{
    public IPromptTemplate PromptTemplate { get; } = promptTemplate;
    
    public override async Task<ChatHistory> RunAsync(IAIServiceConnector serviceConnector, LLMServiceSetting serviceSetting, CancellationToken cancellationToken = default)
    {
        Verifier.NotNull(serviceConnector);
        Verifier.NotNull(serviceSetting);

        AddDefaultValues(LLMWorkEnv.WorkerContext.Variables);

        try
        {
            var prompt = await PromptTemplate.RenderAsync(serviceConnector, cancellationToken).ConfigureAwait(false);
            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(prompt);
            return await serviceConnector.TextCompletionAsync(chatHistory, serviceSetting, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ex.IsCriticalException())
        {
            logger.LogError(ex, "Semantic function {Plugin}.{Name} execution failed with error {Error}",
                PluginName, Name, ex.Message);
            throw;
        }
    }

    public override async IAsyncEnumerable<ChatStreamingResult> RunStreamingAsync(IAIServiceConnector serviceConnector, LLMServiceSetting serviceSetting,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Verifier.NotNull(serviceConnector);
        Verifier.NotNull(serviceSetting);

        var prompt = await PromptTemplate.RenderAsync(serviceConnector, cancellationToken).ConfigureAwait(false);
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt);

        await foreach (var chatStreamingResult in serviceConnector.TextCompletionStreamingAsync(chatHistory, serviceSetting, cancellationToken))
        {
            yield return chatStreamingResult;
        }
    }

    private void AddDefaultValues(VariableDictionary variables)
    {
        foreach (var parameter in Parameters)
        {
            if (!variables.ContainsKey(parameter.Name) && parameter.DefaultValue != null)
            {
                variables[parameter.Name] = parameter.DefaultValue;
            }
        }
    }
}
