using AISmarteasy.Core.Prompt;

namespace AISmarteasy.Core.Function;

public sealed class NativeFunctionConfig(string pluginName, string functionName, IPluginFunction function)
    : FunctionConfig(pluginName, functionName)
{
    public IPluginFunction Function { get; } = function;
}
