using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Function;

public class PluginStore(ILogger logger) : IPluginStore
{
    public Dictionary<string, IPlugin> Plugins { get; } = new(StringComparer.OrdinalIgnoreCase);

    public IPluginFunction? FindFunction(string pluginName, string functionName)
    {
        Verifier.ValidPluginName(pluginName);
        Verifier.ValidFunctionName(functionName);

        Plugins.TryGetValue(pluginName, out var plugin);
        if (plugin != null)
            return plugin.GetFunction(functionName);
        return null;
    }

    public IPlugin? FindPlugin(string pluginName)
    {
        Plugins.TryGetValue(pluginName, out var plugin);
        return plugin;
    }

    public void Register(SemanticFunctionConfig config)
    {
        Verifier.ValidPluginName(config.PluginName);
        Verifier.ValidFunctionName(config.FunctionName);

        var function = CreateSemanticFunction(config);

        if (!Plugins.TryGetValue(config.PluginName, out var plugin))
        {
            plugin = new Plugin(config.PluginName, logger);
            Plugins.Add(config.PluginName, plugin);
        }

        plugin.AddFunction(function);
    }

    private PluginFunction CreateSemanticFunction(SemanticFunctionConfig config)
    {
        if (!config.PromptTemplateConfig.Type.Equals("completion", StringComparison.OrdinalIgnoreCase))
        {
            throw new CoreException($"Function type not supported: {config.PromptTemplateConfig}");
        }

        return new SemanticFunction(config.PluginName, config.FunctionName, config.PromptTemplateConfig.Description, config.PromptTemplate, logger);
    }
}
