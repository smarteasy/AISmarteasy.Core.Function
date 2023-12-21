using AISmarteasy.Core.Prompt;

namespace AISmarteasy.Core.Function;

public abstract class FunctionConfig(string pluginName, string functionName)
{
    public string FunctionName { get; } = functionName;
    public string PluginName { get; } = pluginName;
}
