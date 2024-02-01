namespace AISmarteasy.Core.Function;

public class PluginStore : IPluginStore
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
    public void RegisterPluginFunction(IPluginFunction function)
    {
        string pluginName = function.PluginName;
        if (!Plugins.TryGetValue(pluginName, out var plugin))
        {
            plugin = new Plugin(pluginName);
            Plugins.Add(pluginName, plugin);
        }

        plugin.AddFunction(function);
    }

    private void ValidateFunction(string pluginName, string functionName)
    {
        Verifier.ValidPluginName(pluginName);
        Verifier.ValidFunctionName(functionName);
    }

    private PluginFunction CreateSemanticFunction(SemanticFunctionConfig config)
    {
        if (!config.PromptTemplateConfig.Type.Equals("completion", StringComparison.OrdinalIgnoreCase))
        {
            throw new CoreException($"Function type not supported: {config.PromptTemplateConfig}");
        }

        return new SemanticFunction(config.PluginName, config.FunctionName, config.PromptTemplateConfig.Description, config.PromptTemplate);
    }

    public void BuildSemanticFunctionCategory()
    {
        foreach (var plugin in Plugins.Values)
        {
            var categoryName = plugin.Name;
            var category = new SemanticFunctionCategory(string.Empty, categoryName, string.Empty);
            SemanticFunctionCategories.Add(category);

            foreach (var function in plugin.Functions)
            {
                var fullyQualifiedName = function.ToFullyQualifiedName();
                var content = function.ToManualString();
                
                category.AddSubCategory(new SemanticFunctionCategory(fullyQualifiedName, function.Name, content));
            }
        }
    }
}
