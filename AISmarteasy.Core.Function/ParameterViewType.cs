namespace AISmarteasy.Core.Function;

public class ParameterViewType : IEquatable<ParameterViewType>
{
    private readonly string _name;

    public ParameterViewType(string name)
    {
        Verifier.NotNullOrWhitespace(name, nameof(name));
        _name = name;
    }

    public string Name => _name;

    public override string ToString() => _name;

    public bool Equals(ParameterViewType? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return obj is ParameterViewType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}
