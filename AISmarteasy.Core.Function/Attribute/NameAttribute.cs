namespace AISmarteasy.Core.Function;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
public sealed class NameAttribute : Attribute
{
    public NameAttribute(string name) => Name = name;

    public string Name { get; }
}
