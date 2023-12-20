using AISmarteasy.Core.Prompt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISmarteasy.Core.Function;

public static class SemanticFunctionLoader
{
    private const string SEMANTIC_PLUGIN_CONFIG_FILE = "config.json";
    private const string SEMANTIC_PLUGIN_PROMPT_FILE = "skprompt.txt";
    private static string _pluginDirectory = string.Empty;
    private static PluginStore PluginStore { get; set; } = null!;

    private static ILogger _logger = NullLogger.Instance;

    public static IPluginStore Load(ILogger logger)
    {
        Initialize(logger);
        LoadPlugin();

        return PluginStore;
    }

    private static void Initialize(ILogger logger)
    {
        _pluginDirectory = Path.Combine(Directory.GetCurrentDirectory(), "plugins", "semantic");
        _logger = logger;

        PluginStore = new PluginStore(_logger);
    }

    private static void LoadPlugin()
    {
        string[] subDirectories = Directory.GetDirectories(_pluginDirectory);
        LoadPlugin(subDirectories);
    }

    private static void LoadPlugin(string[] subDirectories)
    {
        foreach (var directory in subDirectories)
        {
            LoadSubPlugin(directory, Directory.GetDirectories(directory));
        }

        LoadSubPlugin(_pluginDirectory, subDirectories);
    }

    private static void LoadSubPlugin(string parentDirectory, string[] subDirectories)
    {
        foreach (var subDirectory in subDirectories)
        {
            var promptPath = Path.Combine(subDirectory, SEMANTIC_PLUGIN_PROMPT_FILE);
            if (File.Exists(promptPath))
            {
                var pluginName = Path.GetFileName(parentDirectory);
                var functionName = Path.GetFileName(subDirectory);
                LoadFunction(pluginName, functionName, subDirectory);
            }
            else
            {
                LoadSubPlugin(subDirectory, Directory.GetDirectories(subDirectory));
            }
        }
    }

    private static void LoadFunction(string pluginName, string functionName, string directoryPath)
    {
        var promptPath = Path.Combine(directoryPath, SEMANTIC_PLUGIN_PROMPT_FILE);
        var configPath = Path.Combine(directoryPath, SEMANTIC_PLUGIN_CONFIG_FILE);

        var config = PromptTemplateConfig.FromJson(File.ReadAllText(configPath));

        Verifier.NotNull(config);
        var template = new PromptTemplate(File.ReadAllText(promptPath), _logger);
        var functionConfig = new SemanticFunctionConfig(pluginName, functionName, config, template);

        PluginStore.Register(functionConfig);

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace($"Config {functionName}: {config.ToJson()}");
            _logger.LogTrace($"Registering function {pluginName}.{functionName}");
        }
    }

}