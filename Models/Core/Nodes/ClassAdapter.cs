namespace Models.Core;

/// <summary>
/// Adapter (wrapper) class to make a POCO (Plain Old Class Object) a Model so that it can be represented in the
/// GUI and in a running simulation.
/// </summary>
public class ClassAdapter : Model
{
    /// <summary>Instance of POCO</summary>
    public object Obj { get; set; }
}
