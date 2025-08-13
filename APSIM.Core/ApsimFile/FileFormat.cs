using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace APSIM.Core;

/// <summary>
/// FileFormat is responsible for reading and writing APSIM JSON formatted files.
/// </summary>
public class FileFormat
{
    /// <summary>Return the current version of JSON used in .apsimx files.</summary>
    public static int JSONVersion => Converter.LatestVersion;

    /// <summary>Create a simulations object by reading the specified filename.</summary>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="errorHandler">Error handler to call on exception</param>
    /// <param name="initInBackground">Initialise on a background thread?</param>
    /// <returns></returns>
    public static Node ReadFromFile<T>(string fileName, Action<Exception> errorHandler = null, bool initInBackground = false)
    {
        return ReadFromFileAndReturnConvertState<T>(fileName, errorHandler, initInBackground).head;
    }

    /// <summary>Create a simulations object by reading the specified filename.</summary>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="errorHandler">Error handler to call on exception</param>
    /// <param name="initInBackground">Initialise on a background thread?</param>
    public static (Node head, bool didConvert, JObject json) ReadFromFileAndReturnConvertState<T>(string fileName, Action<Exception> errorHandler = null, bool initInBackground = false)
    {
        try
        {
            if (!File.Exists(fileName))
                throw new Exception("Cannot read file: " + fileName + ". File does not exist.");

            string contents = File.ReadAllText(fileName);
            return ReadFromStringAndReturnConvertState<T>(contents, errorHandler, initInBackground, fileName);
        }
        catch (Exception err)
        {
            throw new Exception($"Error reading file {fileName}", err);
        }
    }

    /// <summary>Create a simulations object by reading the specified filename.</summary>
    /// <param name="st">The string to convert.</param>
    /// <param name="errorHandler">Error handler to call on exception</param>
    /// <param name="initInBackground">Initialise on a background thread?</param>
    /// <param name="fileName">The optional filename where the string came from. This is required by the converter, when it needs to modify the .db file.</param>
    public static Node ReadFromString<T>(string st, Action<Exception> errorHandler = null, bool initInBackground = false, string fileName = null)
    {
        return ReadFromStringAndReturnConvertState<T>(st, errorHandler, initInBackground, fileName).head;
    }

    /// <summary>Convert a string (json or xml) to a model.</summary>
    /// <param name="st">The string to convert.</param>
    /// <param name="errorHandler">Error handler to call on exception</param>
    /// <param name="initInBackground">Initialise on a background thread?</param>
    /// <param name="fileName">The optional filename where the string came from. This is required by the converter, when it needs to modify the .db file.</param>
    public static (Node head, bool didConvert, JObject json) ReadFromStringAndReturnConvertState<T>(string st, Action<Exception> errorHandler = null, bool initInBackground = false, string fileName = null)
    {
        // Run the converter.
        var converter = Converter.DoConvert(st, -1, fileName);

        JsonSerializerSettings settings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            DateParseHandling = DateParseHandling.None
        };
        INodeModel newModel = JsonConvert.DeserializeObject<T>(converter.Root.ToString(), settings) as INodeModel;

        var head = Node.Create(newModel, errorHandler, initInBackground, fileName);
        return (head, converter.DidConvert, converter.Root);
    }

    /// <summary>Convert a model to a string (json).</summary>
    /// <param name="node">The model to serialise.</param>
    /// <returns>The json string.</returns>
    public static string WriteToString(Node node)
    {
        // Let models know a deserialisation is about to occur
        foreach (var n in node.Walk())
            if (n.Model is ICreatable creatableModel)
                creatableModel.OnSerialising();

        JsonSerializer serializer = new JsonSerializer()
        {
            DateParseHandling = DateParseHandling.None,
            TypeNameHandling = TypeNameHandling.Objects,
            ContractResolver = new WritablePropertiesOnlyResolver(),
            Formatting = Newtonsoft.Json.Formatting.Indented
        };
        string json;
        using (StringWriter s = new StringWriter())
        using (var writer = new JsonTextWriter(s))
        {
            serializer.Serialize(writer, node.Model, node.Model.GetType());
            json = s.ToString();
        }
        return json;
    }

    /// <summary>A contract resolver class to only write settable properties.</summary>
    private class WritablePropertiesOnlyResolver : DefaultContractResolver
    {
        protected override IValueProvider CreateMemberValueProvider(MemberInfo member)
        {
            if (member.Name == "Children")
                return new ChildrenProvider(member);

            return base.CreateMemberValueProvider(member);
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            property.ShouldSerialize = instance =>
            {
                var attributes = member.GetCustomAttributes();
                bool propertyHasLinkOrJsonIgnore = attributes.Any(a => a.GetType().Name == "LinkAttribute" || a.GetType().Name == "JsonIgnoreAttribute");
                if (propertyHasLinkOrJsonIgnore)
                    return false;

                // Serialise public fields.
                if (member is FieldInfo f)
                    return f.IsPublic;

                // Only serialise public properties
                // If a memberinfo has a link, JsonIgnore or is readonly then don't serialise it.
                if (!(member is PropertyInfo property) ||
                    !property.GetMethod.IsPublic ||
                    !property.CanWrite ||
                    property.SetMethod.IsPrivate)
                    return false;

                // If a memberinfo has a description attribute serialise it.
                bool propertyHasDescription = attributes.Any(a => a.GetType().Name == "DescriptionAttribute");
                if (propertyHasDescription)
                    return true;

                // If the instance has come from a resource then don't serialise the member if it has
                // come from the resource (e.g. Definitions from Fertiliser model)
                if (instance is INodeModel model && !string.IsNullOrEmpty(model.ResourceName))
                {
                    var resourceMembers = Resource.Instance.GetPropertiesFromResourceModel(model.ResourceName);
                    if (resourceMembers != null && resourceMembers.Contains(property))
                        return false;
                }

                // Serialise everything else.
                return true;
            };

            return property;
        }

        private class ChildrenProvider : IValueProvider
        {
            private readonly MemberInfo memberInfo;

            public ChildrenProvider(MemberInfo memberInfo)
            {
                this.memberInfo = memberInfo;
            }

            public object GetValue(object target)
            {
                if (target is INodeModel m)
                    return ChildrenToSerialize(m);

                return new ExpressionValueProvider(memberInfo).GetValue(target);
            }

            public void SetValue(object target, object value)
            {
                new ExpressionValueProvider(memberInfo).SetValue(target, value);
            }

            /// <summary>
            /// Gets all child models which are not part of the 'official' model resource.
            /// Generally speaking, this is all models which have been added by the user
            /// (e.g. cultivars).
            /// </summary>
            /// <remarks>
            /// This returns all child models which do not have a matching model in the
            /// resource model's children. A match is defined as having the same name and
            /// type.
            /// </remarks>
            private IEnumerable<INodeModel> ChildrenToSerialize(INodeModel model)
            {
                if (model.GetType().Name == "Manager")
                    return [];

                // Serialise all child if ResourceName is empty.
                if (string.IsNullOrEmpty(model.ResourceName))
                    return model.GetChildren();

                // Return a collection of child models that aren't from a resource.
                return Resource.Instance.RemoveResourceChildren(model);
            }
        }
    }
}
