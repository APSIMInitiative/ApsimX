using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Models.Core.ApsimFile
{

    /// <summary>
    /// A class for reading and writing the .apsimx file format.
    /// </summary>
    /// <remarks>
    /// Features:
    /// * Can WRITE a model in memory to an APSIM Next Generation .json string.
    ///     - Only writes public, settable, properties of a model.
    ///     - If a model implements IDontSerialiseChildren then no child models will be serialised.
    ///     - Won't serialise any property with LinkAttribute.
    /// * Can READ an APSIM Next Generation JSON or XML string to models in memory.
    ///     - Calls converter on the string before deserialisation.
    ///     - Sets fileName property in all simulation models read in.
    ///     - Correctly parents all models.
    ///     - Calls IModel.OnCreated() for all newly created models. If models throw in the
    ///       OnCreated() method, exceptions will be captured and returned to caller along
    ///       with the model tree.
    /// </remarks>
    public class FileFormat
    {
        /// <summary>Convert a model to a string (json).</summary>
        /// <param name="model">The model to serialise.</param>
        /// <returns>The json string.</returns>
        public static string WriteToString(IModel model)
        {
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
                serializer.Serialize(writer, model, model.GetType());
                json = s.ToString();
            }
            return json;
        }

        /// <summary>Create a simulations object by reading the specified filename</summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="errorHandler">Action to be taken when an error occurs.</param>
        /// <param name="initInBackground">Iff set to true, the models' OnCreated() method calls will occur in a background thread.</param>
        /// <param name="compileManagerScripts">If set to true, manager scripts will be compiled as it is loaded. Defaults to true</param>
        public static ConverterReturnType ReadFromFile<T>(string fileName, Action<Exception> errorHandler, bool initInBackground, bool compileManagerScripts = true) where T : IModel
        {
            try
            {
                if (!File.Exists(fileName))
                    throw new Exception("Cannot read file: " + fileName + ". File does not exist.");

                string contents = File.ReadAllText(fileName);
                var converter = ReadFromString<T>(contents, errorHandler, initInBackground, fileName, compileManagerScripts: compileManagerScripts);
                var newModel = converter.NewModel;

                if (newModel is Simulations)
                    (newModel as Simulations).FileName = fileName;
                foreach (Simulation sim in newModel.FindAllDescendants<Simulation>())
                    sim.FileName = fileName;
                converter.NewModel = newModel;
                return converter;
            }
            catch (Exception err)
            {
                throw new Exception($"Error reading file {fileName}", err);
            }
        }

        /// <summary>Convert a string (json or xml) to a model.</summary>
        /// <param name="st">The string to convert.</param>
        /// <param name="errorHandler">Action to be taken when an error occurs.</param>
        /// <param name="initInBackground">Iff set to true, the models' OnCreated() method calls will occur in a background thread.</param>
        /// <param name="fileName">The optional filename where the string came from. This is required by the converter, when it needs to modify the .db file.</param>
        /// <param name="compileManagerScripts">If set to true, manager scripts will be compiled as it is loaded. Defaults to true</param>
        public static ConverterReturnType ReadFromString<T>(string st, Action<Exception> errorHandler, bool initInBackground, string fileName = null, bool compileManagerScripts = true) where T : IModel
        {
            // Run the converter.
            var converter = Converter.DoConvert(st, -1, fileName);

            T newModel;
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto,
                DateParseHandling = DateParseHandling.None
            };
            newModel = JsonConvert.DeserializeObject<T>(converter.Root.ToString(), settings);

            if (newModel is Simulations)
                (newModel as Simulations).FileName = fileName;

            // Parent all models.
            newModel.Parent = null;
            try
            {
                newModel.ParentAllDescendants();
            }
            catch (Exception err)
            {
                errorHandler(err);
            }

            // Replace all models that have a ResourceName with the official, released models.
            Resource.Instance.Replace(newModel);

            // Call created in all models.
            if (initInBackground)
                Task.Run(() => InitialiseModel(newModel, errorHandler, compileManagerScripts));
            else
                InitialiseModel(newModel, errorHandler, compileManagerScripts);

            converter.NewModel = newModel;

            return converter;
        }

        /// <summary>
        /// Initialise a model
        /// </summary>
        /// <param name="newModel"></param>
        /// <param name="errorHandler"></param>
        /// <param name="compileManagers"></param>
        public static void InitialiseModel(IModel newModel, Action<Exception> errorHandler, bool compileManagers = true)
        {
            List<Simulation> simulationList = newModel.FindAllDescendants<Simulation>().ToList();
            foreach (Simulation simulation in simulationList)
                simulation.IsInitialising = true;
            try
            {
                foreach (var model in newModel.FindAllDescendants().ToList())
                {
                    try
                    {
                        model.OnCreated();

                        if (compileManagers)
                            if (model is Manager manager)
                                manager.RebuildScriptModel();
                    }
                    catch (Exception err)
                    {
                        errorHandler(err);
                    }
                }
            }
            finally
            {
                foreach (Simulation simulation in simulationList)
                    simulation.IsInitialising = false;
            }
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
                    if (member.GetCustomAttribute<LinkAttribute>() != null ||
                        member.GetCustomAttribute<JsonIgnoreAttribute>() != null)
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
                    if (member.GetCustomAttribute<DescriptionAttribute>() != null)
                        return true;

                    // If the instance has come from a resource then don't serialise the member if it has
                    // come from the resource (e.g. Definitions from Fertiliser model)
                    if (instance is IModel model && !string.IsNullOrEmpty(model.ResourceName))
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
                    if (target is IModel m)
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
                private IEnumerable<IModel> ChildrenToSerialize(IModel model)
                {
                    if (model is Manager)
                    {
                        List<Model> children = new List<Model>();
                        foreach (Model child in model.Children) {
                            if (child as IScript == null)
                                children.Add(child);
                        }
                        return children.ToArray();
                    }

                    // Serialise all child if ResourceName is empty.
                    if (string.IsNullOrEmpty(model.ResourceName))
                        return model.Children;

                    // Return a collection of child models that aren't from a resource.
                    return Resource.Instance.RemoveResourceChildren(model);
                }
            }
        }
    }
}
