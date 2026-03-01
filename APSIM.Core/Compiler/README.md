# ScriptCompiler

ScriptCompiler encapsulates the ability to compile a c# script into an assembly.
NodeTree maintains a singleton instance.

```c#
/// <summary>Compile a c# script.</summary>
/// <param name="code">The c# code to compile.</param>
/// <param name="node">The node containing the script.</param>
/// <param name="referencedAssemblies">Optional referenced assemblies.</param>
/// <param name="allowDuplicateClassName">Optional to not throw if this has a duplicate class name (used when copying script node)</param>
/// <returns>The result of the compilation.</returns>
public Results Compile(string code, Node node, IEnumerable<MetadataReference> referencedAssemblies = null, bool allowDuplicateClassName = false);
```


