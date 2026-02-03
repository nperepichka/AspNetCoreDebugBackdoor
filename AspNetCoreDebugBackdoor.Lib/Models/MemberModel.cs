namespace AspNetCoreDebugBackdoor.Lib.Models;

public class MemberModel
{
    public string Name { get; set; } = string.Empty;
    public string MemberType { get; set; } = string.Empty; // Property, Field, Method
    public string ReturnType { get; set; } = string.Empty;
    public bool IsStatic { get; set; }
    public bool IsPublic { get; set; }
    public object? Value { get; set; } // Null if not evaluated or not applicable
    public string? ValueType { get; set; } // "null", "primitive", "string", "collection", "complex"
    public bool IsEditable { get; set; }
    public string? Error { get; set; } // If evaluation failed
    public List<ParameterModel>? Parameters { get; set; } // For Methods/Constructors

    public MemberModel() { }
}
