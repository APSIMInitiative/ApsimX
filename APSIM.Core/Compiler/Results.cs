using System.Reflection;

namespace APSIM.Core;

/// <summary>Encapsulates results from a compile.</summary>
public class Results
{
    private readonly Assembly compiledAssembly;
    private readonly string instanceTypeName;

    /// <summary>Constructor.</summary>
    public Results(Assembly assembly, string typeName, bool newlyCompiled)
    {
        compiledAssembly = assembly;
        instanceTypeName = typeName;
        WasCompiled = newlyCompiled;
    }

    /// <summary>Constructor.</summary>
    public Results(string errors)
    {
        WasCompiled = true;
        ErrorMessages = errors;
    }

    /// <summary>Compile error messages. Null for no errors.</summary>
    public string ErrorMessages { get; }

    /// <summary>Was the script compiled or was it already up to date?</summary>
    public bool WasCompiled { get; }

    /// <summary>A newly created instance.</summary>
    public object Instance { get { return compiledAssembly.CreateInstance(instanceTypeName); } }
}
