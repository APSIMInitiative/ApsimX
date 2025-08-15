# Resources

Resources encapsulates methods for accessing model resources (JSON files stored in
the Models assembly). Resources

```c#
/// <summary>
/// Get a collection of child models that are from a resource.
/// </summary>
/// <param name="parentModel">The parent model to search for.</param>
public IEnumerable<INodeModel> GetChildModelsThatAreFromResource(INodeModel parentModel);

/// <summary>Get a model from resource.</summary>
/// <param name="resourceName">Name of model.</param>
/// <returns>The newly created model. Throws if not found.</returns>
public INodeModel GetModel(string resourceName);

/// <summary>Get a model resource as a string.</summary>
/// <param name="resourceName">Name of the resource.</param>
/// <returns>The model JSON string. Throws if not found.</returns>
public static string GetString(string resourceName);


```


