using AISmarteasy.Core.Prompt;

namespace AISmarteasy.Core.Function;

public sealed class SemanticFunctionConfig(string pluginName, string functionName, PromptTemplateConfig config, IPromptTemplate template)
    : FunctionConfig(pluginName, functionName)
{
    public PromptTemplateConfig PromptTemplateConfig { get; } = config;
    public IPromptTemplate PromptTemplate { get; } = template;
}
