using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Function;

public class PluginStore(ILogger logger) : IPluginStore
{
    public Dictionary<string, IPlugin> Plugins { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<SemanticFunctionCategory> SemanticFunctionCategories { get; } = new();

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
        ValidateFunction(config.PluginName, config.FunctionName);
        var function = CreateSemanticFunction(config);
        RegisterPluginFunction(function);
    }

    private void ValidateFunction(string pluginName, string functionName)
    {
        Verifier.ValidPluginName(pluginName);
        Verifier.ValidFunctionName(functionName);
    }

    private void RegisterPluginFunction(IPluginFunction function)
    {
        string pluginName = function.PluginName;
        if (!Plugins.TryGetValue(pluginName, out var plugin))
        {
            plugin = new Plugin(pluginName, logger);
            Plugins.Add(pluginName, plugin);
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

    public void BuildSemanticFunctionCategory()
    {
        foreach (var plugin in Plugins.Values)
        {
            var categoryName = plugin.Name;
            var category = new SemanticFunctionCategory(categoryName, string.Empty, logger);
            SemanticFunctionCategories.Add(category);

            foreach (var function in plugin.Functions)
            {
                var content  = string.Join("\n\n", function.Info.ToManualString());
                content += "\n\n";

                category.AddSubCategory(new SemanticFunctionCategory(function.Name, content, logger));
            }
        }
    }
}
