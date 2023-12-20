namespace AISmarteasy.Core.Function;

public abstract class PluginFunction : IPluginFunction
{
    public PluginFunctionInfo Info { get; init; }

    public string Name => Info.Name;
    public string PluginName => Info.PluginName;

    public abstract Task<ChatHistory> RunAsync(IAIServiceConnector serviceConnector, LLMServiceSetting serviceSetting, CancellationToken cancellationToken = default);

    public string Description => Info.Description;
    
    public IList<ParameterInfo> Parameters { get; set; }

    public LLMServiceSetting ServiceSetting { get; set; } = new();

    protected PluginFunction(string pluginName, string name, string description, bool isSemantic, IList<ParameterInfo> parameters)
    {
        Verifier.ParametersUniqueness(parameters);

        Info = new PluginFunctionInfo(pluginName, name, description, isSemantic, parameters);
        Parameters = Info.Parameters;
    }

    protected PluginFunction()
    : this(string.Empty, string.Empty, string.Empty, true, new List<ParameterInfo>())
    {
    }
}
