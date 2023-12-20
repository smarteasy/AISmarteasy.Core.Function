using AISmarteasy.Core.Prompt;

namespace AISmarteasy.Core.Function;

public sealed class SemanticFunctionConfig(string pluginName, string functionName, PromptTemplateConfig config, IPromptTemplate template)
{
    public string FunctionName { get; } = functionName;
    public string PluginName { get; } = pluginName;
    public PromptTemplateConfig PromptTemplateConfig { get; } = config;
    public IPromptTemplate PromptTemplate { get; } = template;
}
