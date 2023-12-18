using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Function;

public class Plugin(string name, ILogger logger)
{
    public string Name { get; init; } = name;

    private readonly ILogger _logger = logger;
    private readonly Dictionary<string, PluginFunction> _functions = new(StringComparer.OrdinalIgnoreCase);

    public PluginFunction GetFunction(string functionName)
    {
        if (!_functions.TryGetValue(functionName, out var function))
        {
            ThrowFunctionNotAvailable(functionName);
        }

        return function!;
    }

    public void AddFunction(PluginFunction function)
    {
        Verifier.NotNull(function);

        _functions[function.Name] = function;
    }

    public List<PluginFunction> Functions => _functions.Values.ToList();

    private void ThrowFunctionNotAvailable(string functionName)
    {
        _logger.LogError("Function not available: {0}", functionName);

        throw new CoreException($"Function not available {functionName}");
    }
}
