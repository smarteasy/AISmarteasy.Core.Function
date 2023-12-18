namespace AISmarteasy.Core.Function;

public sealed record PluginFunctionInfo(
    string PluginName,
    string Name,
    string Description = "",
    bool IsSemantic = false,
    IList<ParameterInfo>? Parameters = null)
{
    public IList<ParameterInfo> Parameters { get; init; } = Parameters ?? Array.Empty<ParameterInfo>();

    public string ToManualString()
    {
        var inputs = string.Join("\n", Parameters.Select(parameter =>
        {
            var defaultValueString = string.IsNullOrEmpty(parameter.DefaultValue) ? string.Empty : $" (default value: {parameter.DefaultValue})";
            return $"  - {parameter.Name}: {parameter.Description}{defaultValueString}";
        }));

        return $@"{ToFullyQualifiedName()}:
  description: {Description}
  inputs:
  {inputs}";
    }

    public string ToFullyQualifiedName()
    {
        return $"{PluginName}.{Name}";
    }
}