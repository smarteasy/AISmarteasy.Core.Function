using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Function;

public class Plugin(string name) : IPlugin
{
    public string Name { get; init; } = name;

    public List<IPluginFunction> Functions => _functions.Values.ToList();

    private readonly Dictionary<string, IPluginFunction> _functions = new();

    public IPluginFunction? GetFunction(string functionName)
    {
        if (!_functions.TryGetValue(functionName, out var function))
        {
            ThrowFunctionNotAvailable(functionName);
        }

        return function;
    }

    public void AddFunction(IPluginFunction function)
    {
        Verifier.NotNull(function);

        _functions[function.Name] = function;
    }

    private void ThrowFunctionNotAvailable(string functionName)
    {
        var logger = LoggerProvider.Provide();
        logger.LogError("Function not available: {0}", functionName);
        throw new CoreException($"Function not available {functionName}");
    }
}
