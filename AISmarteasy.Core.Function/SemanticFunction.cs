using AISmarteasy.Core.Prompt;
using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Function;

public class SemanticFunction(string pluginName, string name, string description, IPromptTemplate promptTemplate, ILogger logger)
    : PluginFunction(pluginName, name, description, true, new List<ParameterInfo>())
{
    public IPromptTemplate PromptTemplate { get; } = promptTemplate;

    public override Task RunAsync(LLMServiceSetting serviceSetting, CancellationToken cancellationToken = default)
    {
        var kernel = KernelProvider.Kernel;
        AddDefaultValues(kernel.Context.Variables);
        return RunAsync(kernel.TextCompletionService, serviceSetting, cancellationToken);
    }

    public async Task RunAsync(ITextCompletionService textCompletionService, LLMServiceSetting serviceSetting, CancellationToken cancellationToken = default)
    {
        Verifier.NotNull(textCompletionService);
        Verifier.NotNull(serviceSetting);

        var context = KernelProvider.Kernel.Context;

        try
        {
            if (textCompletionService.ServiceType==AIServiceTypeKind.TextCompletion)
            {
                var prompt = await PromptTemplate.RenderAsync(cancellationToken).ConfigureAwait(false);
                var answer = await textCompletionService.RunAsync(prompt, serviceSetting, cancellationToken).ConfigureAwait(false);
                context.Variables.Update(answer);
            }
            else if (textCompletionService.ServiceType == AIServiceTypeKind.ChatCompletion)
            {
                //var prompt = await PromptTemplate.RenderAsync(cancellationToken).ConfigureAwait(false);
                //var chatHistory = new ChatHistory();
                //chatHistory.AddUserMessage(prompt);
                //var chtHistory = await client
                //    .RunChatCompletionAsync(chatHistory, requestSettings, cancellationToken)
                //    .ConfigureAwait(false);
                //context.Variables.Update(chtHistory.Messages[1].Content);
            }
        }
        catch (Exception ex) when (!ex.IsCriticalException())
        {
            logger.LogError(ex, "Semantic function {Plugin}.{Name} execution failed with error {Error}", 
                PluginName, Name, ex.Message);
            throw;
        }
    }

    private void AddDefaultValues(ContextVariableDictionary variables)
    {
        foreach (var parameter in Parameters)
        {
            if (!variables.ContainsKey(parameter.Name) && parameter.DefaultValue != null)
            {
                variables[parameter.Name] = parameter.DefaultValue;
            }
        }
    }

    //public static PluginFunction FromSemanticConfig(string pluginName, string functionName, SemanticFunctionConfig functionConfig,
    //    ILoggerFactory? loggerFactory = null, CancellationToken cancellationToken = default)
    //{
    //    Verifier.NotNull(functionConfig);
    //    Verifier.ParametersUniqueness(functionConfig.PromptTemplate.Parameters);

    //    return new SemanticFunction(functionConfig.PromptTemplate, pluginName, functionName, functionConfig.PromptTemplateConfig.Description, loggerFactory);
    //}
}
