namespace AISmarteasy.Core.Function;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ParameterAttribute : Attribute
{
    public ParameterAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public string Name { get; }

    public string Description { get; }

    public string? DefaultValue { get; set; }
}
