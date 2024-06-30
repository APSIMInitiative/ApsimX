namespace APSIM.Shared.Utilities
{
    using DeepCloner.Core;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Loader;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    /// <summary>
    /// Utility class with reflection functions
    /// </summary>
    public class ReflectionUtilities
    {
        /// <summary>
        /// Returns true if the specified type T is of type TypeName
        /// </summary>
        public static bool IsOfType(Type t, string typeName)
        {
            while (t != null)
            {
                if (t.ToString() == typeName)
                    return true;

                if (t.GetInterface(typeName) != null)
                    return true;

                t = t.BaseType;
            }
            return false;
        }

        /// <summary>
        /// Return all fields. The normal .NET reflection doesn't return private fields in base classes.
        /// This function does.
        /// </summary>
        public static List<FieldInfo> GetAllFields(Type type, BindingFlags flags)
        {
            if (type == null || type == typeof(Object)) return new List<FieldInfo>();

            var list = GetAllFields(type.BaseType, flags);
            // in order to avoid duplicates, force BindingFlags.DeclaredOnly
            list.AddRange(type.GetFields(flags | BindingFlags.DeclaredOnly));
            return list;
        }

        /// <summary>
        /// Return all properties. The normal .NET reflection doesn't return private fields in base classes.
        /// This function does.
        /// </summary>
        public static List<PropertyInfo> GetAllProperties(Type type, BindingFlags flags, bool includeBase)
        {
            var list = new List<PropertyInfo>();
            if (type == typeof(Object) || type == null) 
                return list;

            if (includeBase)
                list = GetAllProperties(type.BaseType, flags, includeBase);

            // in order to avoid duplicates, force BindingFlags.DeclaredOnly
            list.AddRange(type.GetProperties(flags | BindingFlags.DeclaredOnly));
            return list;
        }

        /// <summary>
        /// Return all methods. The normal .NET reflection doesn't return private methods in base classes.
        /// This function does.
        /// </summary>
        public static List<MethodInfo> GetAllMethods(Type type, BindingFlags flags, bool includeBase)
        {
            var list = new List<MethodInfo>();
            if (type == typeof(Object) || type == null)
                return list;

            if (includeBase)
                list = GetAllMethods(type.BaseType, flags, includeBase);

            // in order to avoid duplicates, force BindingFlags.DeclaredOnly
            list.AddRange(type.GetMethods(flags | BindingFlags.DeclaredOnly).ToList());
            return list;
        }

        /// <summary>
        /// Return all methods, with property methods removed from the list.
        /// </summary>
        public static List<MethodInfo> GetAllMethodsWithoutProperties(Type type)
        {
            List<MethodInfo> methods = ReflectionUtilities.GetAllMethods(type, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, false);
            List<PropertyInfo> properties = ReflectionUtilities.GetAllProperties(type, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, false);

            //remove methods with < or > in the name, these don't actually exist in the source.
            for (int i = methods.Count - 1; i >= 0; i--)
            {
                string name = methods[i].Name;
                if (name.Contains("<") || name.Contains(">"))
                {
                    methods.Remove(methods[i]);
                }
            }

            //remove properties from methods list
            foreach (PropertyInfo prop in properties)
            {
                for (int i = methods.Count - 1; i >= 0; i--)
                {
                    string name = methods[i].Name;
                    if (name.CompareTo("get_" + prop.Name) == 0 || name.CompareTo("set_" + prop.Name) == 0)
                    {
                        methods.Remove(methods[i]);
                    }
                }
            }
            return methods;
        }

        /// <summary>
        /// Return all methods, with property methods removed from the list.
        /// </summary>
        public static List<MethodInfo> GetAllMethodsForProperty(Type type, PropertyInfo property)
        {
            List<MethodInfo> methods = ReflectionUtilities.GetAllMethods(type, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, false);
            List<MethodInfo> propMethods = new List<MethodInfo>();

            for (int i = 0; i < methods.Count; i++)
            {
                string name = methods[i].Name;
                if (name.CompareTo("get_" + property.Name) == 0 || name.CompareTo("set_" + property.Name) == 0)
                {
                    propMethods.Add(methods[i]);
                }
            }
            return methods;
        }

        /// <summary>
        /// Get the value of a field or property.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static object GetValueOfFieldOrProperty(string name, object obj)
        {
            int Pos = name.IndexOf('.');
            if (Pos > -1)
            {
                string FieldName = name.Substring(0, Pos);
                obj = GetValueOfFieldOrProperty(FieldName, obj);
                if (obj == null)
                    return null;
                else
                    return GetValueOfFieldOrProperty(name.Substring(Pos + 1), obj);
            }
            else
            {
                BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
                FieldInfo F = obj.GetType().GetField(name, Flags);
                if (F != null)
                    return F.GetValue(obj);

                PropertyInfo P = obj.GetType().GetProperty(name, Flags);
                if (P != null)
                    return P.GetValue(obj, null);

                return null;
            }
        }

        /// <summary>
        /// Trys to set the value of a public or private field or property. Name can have '.' characters. Will
        /// return true if successfull. Will throw if Value is the wrong type for the field
        /// or property. Supports strings/double/int conversion or direct setting.
        /// </summary>
        public static bool SetValueOfFieldOrProperty(string name, object obj, object value)
        {
            if (name.Contains("."))
            {
                int Pos = name.IndexOf('.');
                string FieldName = name.Substring(0, Pos);
                obj = SetValueOfFieldOrProperty(FieldName, obj, value);
                if (obj == null)
                    return false;
                else
                    return SetValueOfFieldOrProperty(name.Substring(Pos + 1), obj, value);
            }
            else
            {
                BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                FieldInfo F = obj.GetType().GetField(name, Flags);
                if (F != null)
                {
                    if (F.FieldType == typeof(string))
                        F.SetValue(obj, value.ToString());
                    else if (F.FieldType == typeof(double))
                        F.SetValue(obj, Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture));
                    else if (F.FieldType == typeof(int))
                        F.SetValue(obj, Convert.ToInt32(value));
                    else
                        F.SetValue(obj, value);
                    return true;
                }

                return SetValueOfProperty(name, obj, value);
            }
        }

        /// <summary>
        /// Set the value of a object property using reflection. Property must be public.
        /// </summary>
        /// <param name="name">Name of the property</param>
        /// <param name="obj">Object to probe</param>
        /// <param name="value">The value to set the property to</param>
        /// <returns>True if value set successfully</returns>
        public static bool SetValueOfProperty(string name, object obj, object value)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase;
            PropertyInfo P = obj.GetType().GetProperty(name, flags);
            if (P != null)
            {
                if (value == null)
                    P.SetValue(obj, value, null);
                else if (P.PropertyType == typeof(string))
                    P.SetValue(obj, value.ToString(), null);
                else if (P.PropertyType == typeof(double))
                    P.SetValue(obj, Convert.ToDouble(value, CultureInfo.InvariantCulture), null);
                else if (P.PropertyType == typeof(int))
                    P.SetValue(obj, Convert.ToInt32(value, CultureInfo.InvariantCulture), null);
                else
                    P.SetValue(obj, value, null);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets all Type instances matching the specified class name with no namespace qualified class name.
        /// Will not throw. May return empty array.
        /// </summary>
        public static Type[] GetTypeWithoutNameSpace(string className, Assembly assembly)
        {
            List<Type> returnVal = new List<Type>();

            Type[] assemblyTypes = assembly.GetTypes();
            for (int j = 0; j < assemblyTypes.Length; j++)
            {
                if (assemblyTypes[j].Name == className)
                {
                    returnVal.Add(assemblyTypes[j]);
                }
            }

            return returnVal.ToArray();
        }

        /// <summary>
        /// Gets the specified attribute type.
        /// </summary>
        /// <returns>Returns the attribute or null if not found.</returns>
        public static Attribute GetAttribute(Type t, Type attributeTypeToFind, bool lookInBaseClasses)
        {
            foreach (Attribute A in t.GetCustomAttributes(lookInBaseClasses))
            {
                if (A.GetType() == attributeTypeToFind)
                    return A;
            }
            return null;
        }

        /// <summary>
        /// Gets the specified attribute type.
        /// </summary>
        /// <returns>Returns the attribute or null if not found.</returns>
        public static Attribute GetAttribute(MemberInfo t, Type attributeTypeToFind, bool lookInBaseClasses)
        {
            foreach (Attribute A in t.GetCustomAttributes(lookInBaseClasses))
            {
                // Attributes can be derived from attributeTypeToFind e.g. ChildLink is derived from Link
                if (attributeTypeToFind.IsAssignableFrom(A.GetType()))
                    return A;
            }
            return null;
        }

        /// <summary>
        /// Gets 0 or more attributes of the specified type.
        /// </summary>
        /// <returns>Returns the attributes or string[0] if none found.</returns>
        public static Attribute[] GetAttributes(Type t, Type attributeTypeToFind, bool lookInBaseClasses)
        {
            List<Attribute> Attributes = new List<Attribute>();
            foreach (Attribute A in t.GetCustomAttributes(lookInBaseClasses))
            {
                if (A.GetType() == attributeTypeToFind)
                    Attributes.Add(A);
            }
            return Attributes.ToArray();
        }

        /// <summary>
        /// Gets 0 or more attributes of the specified type.
        /// </summary>
        /// <returns>Returns the attributes or string[0] if none found.</returns>
        public static Attribute[] GetAttributes(MemberInfo t, Type attributeTypeToFind, bool lookInBaseClasses)
        {
            List<Attribute> Attributes = new List<Attribute>();
            foreach (Attribute A in t.GetCustomAttributes(lookInBaseClasses))
            {
                if (A.GetType() == attributeTypeToFind)
                    Attributes.Add(A);
            }
            return Attributes.ToArray();
        }

        /// <summary>
        /// Returns the name of the specified object if it has a public name property
        /// or it returns the name of the type if no name property is present.
        /// </summary>
        public static string Name(object obj)
        {
            if (obj != null)
            {
                PropertyInfo NameProperty = obj.GetType().GetProperty("Name");
                if (NameProperty == null)
                    return obj.GetType().Name;
                else
                    return NameProperty.GetValue(obj, null) as string;
            }
            return null;
        }

        /// <summary>
        /// Sets the name of the specified object if it has a public name property that is settable.
        /// Will throw if cannot set the name.
        /// </summary>
        public static void SetName(object obj, string newName)
        {
            PropertyInfo NameProperty = obj.GetType().GetProperty("Name");
            if (NameProperty == null || !NameProperty.CanWrite)
                throw new Exception("Cannot set the name of object with type: " + obj.GetType().Name +
                                    ". It does not have a public, settable, name property");
            else
                NameProperty.SetValue(obj, newName, null);
        }

        /// <summary>
        /// Returns true if the specified object has a name property with a public setter.
        /// </summary>
        public static bool NameIsSettable(object obj)
        {
            PropertyInfo NameProperty = obj.GetType().GetProperty("Name");
            return NameProperty != null && NameProperty.CanWrite;
        }

        /// <summary>
        /// Return a type from the specified unqualified (no namespaces) type name.
        /// </summary>
        public static Type GetTypeFromUnqualifiedName(string typeName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types = new Type[0];
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException)
                {
                    continue;
                }
                foreach (Type type in types)
                {
                    if (type.Name == typeName)
                        return type;
                }
            }
            return null;
        }

        /// <summary>
        /// Convert an object into a json string. 
        /// </summary>
        /// <param name="source">The source object.</param>
        /// <param name="includePrivates">Serialise private members as well as publics?</param>
        /// <param name="includeChildren">Serialize child models as well?</param>
        /// <returns>The string representation of the object.</returns>
        public static string JsonSerialise(object source, bool includePrivates, bool includeChildren = true)
        {
            return JsonConvert.SerializeObject(source, Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new DynamicContractResolver(includePrivates, includeChildren, false),
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });
        }

        /// <summary>
        /// Convert an object into a json stream. 
        /// </summary>
        /// <param name="source">The source object.</param>
        /// <returns>The string representation of the object.</returns>
        public static Stream JsonSerialiseToStream(object source)
        {
            JsonSerializerSettings jSettings = new JsonSerializerSettings()
            {
                ContractResolver = new DynamicContractResolver(true, true, true),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Objects,
                NullValueHandling = NullValueHandling.Include,
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                DefaultValueHandling = DefaultValueHandling.Include
            };
            string jString = JsonConvert.SerializeObject(source, Formatting.Indented, jSettings);
            MemoryStream jStream = new MemoryStream();
            StreamWriter writer = new StreamWriter(jStream);
            writer.Write(jString);
            writer.Flush();

            jStream.Position = 0; ;
            return jStream;
            
        }
        
        /// <summary>
        /// Convert a JSON stream into an object
        /// </summary>
        /// <param name="jStream"></param>
        /// <returns></returns>
        public static object JsonDeserialise(Stream jStream)
        {
            if (jStream == null)
                return null;

            using (StreamReader sr = new StreamReader(jStream))
            {
                string jString = sr.ReadToEnd();
                JsonSerializerSettings jSettings = new JsonSerializerSettings()
                {
                    ContractResolver = new DynamicContractResolver(true, true, true),
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Objects,
                    NullValueHandling = NullValueHandling.Include,
                    ObjectCreationHandling = ObjectCreationHandling.Replace,
                    DefaultValueHandling = DefaultValueHandling.Include,
                    ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
                };
                object result = JsonConvert.DeserializeObject(jString, jSettings);
                // Newtonsoft Json cannot readily determine the object type when
                // deserializing a DataTable. If the DataTable is a field within another object,
                // tagging with [JsonConverter(typeof(DataTableConverter))] fixes the problem, but 
                // a stand-alone DataTable is problematic, and will just deserialize to a JArray.
                // This can be a problem in APSIM.Server and associated unit tests. Here we do
                // a clumsy work-around by assuming a returned JArray should really be treated
                // as a DataTable, and force it to be deserialized as such. Note, however, that
                // the Name of the DataTable is not available with this procedure.
                // This should work with our limited use of JSON in the context of APSIM.Server,
                // but could be a problem if applied in other contexts.
                if (result is JArray)
                    return JsonConvert.DeserializeObject<System.Data.DataTable>(jString);
                else
                    return result;
            } 
        }

        ///<summary> Custom Contract resolver to stop deseralization of Parent properties </summary>
        private class DynamicContractResolver : DefaultContractResolver
        {
            private BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
            private readonly bool includeChildren;
            private readonly bool excludeReadonly;

            public DynamicContractResolver(bool includePrivates, bool includeChildren, bool excludeReadonly)
            {
                this.includeChildren = includeChildren;
                this.excludeReadonly = excludeReadonly;
                if (includePrivates)
                    bindingFlags |= BindingFlags.NonPublic;
            }

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                IEnumerable<JsonProperty> fields = GetAllFields(type, bindingFlags).Select(p => base.CreateProperty(p, memberSerialization));
                IEnumerable<JsonProperty> properties = GetAllProperties(type, bindingFlags, true).Select(p => base.CreateProperty(p, memberSerialization));
                if (!includeChildren)
                    properties = properties.Where(p => p.PropertyName != "Children");
                List<JsonProperty> props = fields.Union(properties).ToList();
                if (excludeReadonly)
                    props = props.Where(p => p.Writable).ToList();

                // If this type overrides a base class's property or field, then this list
                // will contain multiple properties with the same name, which causes a
                // serialization exception when we go to serialize these properties. The
                // solution is to group the properties by name and take the last of each
                // group so we end up with the most derived property.
                props = props.GroupBy(p => p.PropertyName).Select(g => g.Last()).ToList();
                props.ForEach(p => { p.Writable = true; });
                return props.Where(p => p.PropertyName != "Parent" && p.Readable).ToList();
            }

            protected override JsonContract CreateContract(Type objectType)
            {
                JsonContract contract = base.CreateContract(objectType);

                return contract;
            }

        }

        /// <summary>
        /// Convert the specified 'stringValue' into an object of the specified 'type'
        /// using the invariant culture. Will throw if cannot convert type.
        /// </summary>
        public static object StringToObject(Type dataType, string newValue)
        {
            return StringToObject(dataType, newValue, CultureInfo.InvariantCulture);
        }

        private static readonly HashSet<Type> numericTypes = new HashSet<Type>
        {
            typeof(decimal),
            typeof(short), typeof(ushort),
            typeof(int), typeof(uint),
            typeof(long), typeof(ulong),
            typeof(float), typeof(double)
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataType"></param>
        /// <returns></returns>
        public static bool IsNumericType(Type dataType)
        {
            return numericTypes.Contains(dataType);
        }

        /// <summary>
        /// Convert the specified 'stringValue' into an object of the specified 'type'.
        /// Will throw if cannot convert type.
        /// </summary>
        public static object StringToObject(Type dataType, string newValue, IFormatProvider format)
        {
            if (string.IsNullOrWhiteSpace(newValue))
            {
                // Empty string. Get the default value for this property type.
                if (dataType.IsValueType)
                {
                    if (dataType == typeof(double))
                        return double.NaN;

                    if (dataType == typeof(float))
                        return float.NaN;

                    // Property is not nullable (could be int, bool, struct, etc).
                    // Return default value for this type.
                    return Activator.CreateInstance(dataType);
                }
                else
                    // Property is nullable so return null.
                    return null;
            }

            if ( (dataType.IsArray || typeof(IEnumerable<>).IsAssignableFrom(dataType) || typeof(IEnumerable).IsAssignableFrom(dataType)) && dataType != typeof(string))
            {
                // Arrays do not implement IConvertible, so we cannot just split the string on
                // the commas and parse the string array into Convert.ChangeType. Instead, we
                // must convert each element of the array individually.
                //
                // Note: we trim the start of each element, so "a, b, c , d" becomes ["a","b","c ","d"].
                Type elementType;
                if (dataType.IsArray)
                    elementType = dataType.GetElementType();
                else if (dataType.IsGenericType)
                    elementType = dataType.GenericTypeArguments.First();
                else
                    elementType = typeof(object);
                object[] arr = newValue.Split(',').Select(s => StringToObject(elementType, s.TrimStart(), format)).ToArray();

                // arr is an array of object. We need an array with correct element type.
                if (dataType.IsArray)
                {
                    Array result = Array.CreateInstance(elementType, arr.Length);
                    Array.Copy(arr, result, arr.Length);
                    return result;
                }
                else
                {
                    Type listType = typeof(List<>).MakeGenericType(elementType);
                    IList list = (IList)Activator.CreateInstance(listType);
                    foreach (object obj in arr)
                        list.Add(obj);
                    return list;
                }
            }

            if (dataType == typeof(System.Drawing.Color) && int.TryParse(newValue, out int argb))
                return System.Drawing.Color.FromArgb(argb);

            // Do we really want enums to be case-insensitive?
            if (dataType.IsEnum)
                return Enum.Parse(dataType, newValue, true);

            // Bools as ints - special case
            if (dataType == typeof(int))
            {
                if (newValue.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                    return true;
                if (newValue.Equals("false", StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }

            // Convert.ChangeType() doesn't seem to work properly on nullable types.
            Type underlyingType = Nullable.GetUnderlyingType(dataType);
            if (underlyingType != null)
                dataType = underlyingType;

            try
            {
                return Convert.ChangeType(newValue, dataType, format);
            }
            catch (Exception err)
            {
                throw new FormatException($"Unable to convert {newValue} to type {dataType}", err);
            }
        }

        /// <summary>
        /// Convert the specified 'obj' into a string using the
        /// invariant culture.
        /// </summary>
        /// <param name="obj">Object to be converted.</param>
        public static string ObjectToString(object obj)
        {
            return ObjectToString(obj, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Convert the specified 'obj' into a string.
        /// </summary>
        /// <param name="obj">Object to be converted.</param>
        /// <param name="format">Culture to use for the conversion.</param>
        public static string ObjectToString(object obj, IFormatProvider format)
        {
            if (obj == null)
                return null;

            if (obj.GetType().IsArray)
            {
                string stringValue = "";
                Array arr = obj as Array;
                for (int j = 0; j < arr.Length; j++)
                {
                    if (j > 0)
                        stringValue += ", ";
                    stringValue += ObjectToString(arr.GetValue(j));
                }
                return stringValue;
            }
            else if (obj.GetType() == typeof(System.Drawing.Color))
                return ((System.Drawing.Color)obj).ToArgb().ToString();
            else
                return Convert.ToString(obj, format);
        }

        /// <summary>
        /// Perform a deep Copy of the specified object
        /// </summary>
        public static object Clone(object sourceObj)
        {
            DeepClonerExtensions.SetSuppressedAttributes(typeof(NonSerializedAttribute));
            return sourceObj.DeepClone();
        }

        /// <summary>
        /// Custom SerializationBinder that records the assemblies seen during serialisation
        /// and reuses them during deserialisation.
        /// This is useful when working with assemblies from a non-default AssemblyLoadContext,
        /// because BinaryFormatter cannot deserialise them otherwise.
        /// </summary>
        class CachingSerializationBinder : SerializationBinder
        {
            private Dictionary<string, Assembly> assemblyCache = new Dictionary<string, Assembly>();

            public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                assemblyName = serializedType.Assembly.FullName;
                typeName = serializedType.FullName;
                if (!assemblyCache.ContainsKey(assemblyName))
                {
                    assemblyCache[assemblyName] = serializedType.Assembly;
                }
                else
                {
                    if (assemblyCache[assemblyName] != serializedType.Assembly)
                    {
                        throw new FileLoadException(String.Format("Assemblies with the same name from different load contexts are not supported: '{0}'.", assemblyName));
                    }
                }
            }

            public override Type BindToType(string assemblyName, string typeName)
            {
                string qualifiedTypeName = String.Format("{0}, {1}", typeName, assemblyName);
                return Type.GetType(qualifiedTypeName, assemblyResolver: ResolveAssembly, typeResolver: null);
            }

            private Assembly ResolveAssembly(AssemblyName assemblyName)
            {
                if (assemblyCache.ContainsKey(assemblyName.FullName))
                {
                    return assemblyCache[assemblyName.FullName];
                }
                else
                {
                    return AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
                }
            }
        }

        /// <summary>
        /// Return a list of sorted properties.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static PropertyInfo[] GetPropertiesSorted(Type type, BindingFlags flags)
        {
            List<PropertyInfo> properties = new List<PropertyInfo>();

            properties.AddRange(type.GetProperties(flags));
            properties.Sort(new PropertyInfoComparer());

            return properties.ToArray();

        }

        /// <summary>
        /// A private property comparer.
        /// </summary>
        private class PropertyInfoComparer : IComparer<PropertyInfo>
        {
            // Calls CaseInsensitiveComparer.Compare with the parameters reversed.
            public int Compare(PropertyInfo x, PropertyInfo y)
            {
                return x.Name.CompareTo(y.Name);
            }
        }

        /// <summary>
        /// A type comparer.
        /// </summary>
        public class TypeComparer : IComparer<Type>
        {
            /// <summary>A type comparer that uses names.</summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            public int Compare(Type x, Type y)
            {
                return x.Name.CompareTo(y.Name);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <returns></returns>
        public static List<Type> GetTypesThatHaveInterface(Type interfaceType)
        {
            List<Type> types = new List<Type>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                types.AddRange(GetTypesThatHaveInterface(assembly, interfaceType));

            return types;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="interfaceType"></param>
        /// <returns></returns>
        public static IEnumerable<Type> GetTypesThatHaveInterface(Assembly assembly, Type interfaceType)
        {
            List<Type> types = new List<Type>();

            foreach (Type t in assembly.GetTypes())
                if (interfaceType.IsAssignableFrom(t) && t.Name != interfaceType.Name && t.IsPublic)
                    types.Add(t);

            return types;
        }

        /// <summary>
        /// Convert an enum value to a string. Looks for an attribute and uses that if found.
        /// </summary>
        public static string EnumToString<T>(T enumerationValue) where T : struct
        {
            Type type = enumerationValue.GetType();
            if (!type.IsEnum)
                throw new ArgumentException("EnumerationValue must be of Enum type", "enumerationValue");

            // Tries to find a DescriptionAttribute for a potential friendly name
            // for the enum
            MemberInfo[] memberInfo = type.GetMember(enumerationValue.ToString());
            if (memberInfo != null && memberInfo.Length > 0)
            {
                object[] attrs = memberInfo[0].GetCustomAttributes(inherit: false);

                if (attrs != null && attrs.Length > 0)
                    return (attrs[0]).ToString();
            }
            //If we have no description attribute, just return the ToString of the enum
            return enumerationValue.ToString();
        }


        /// <summary>
        /// Get a string from a resource file stored in the current assembly.
        /// </summary>
        /// <param name="resourceName">Name of the resource.</param>
        public static string GetResourceAsString(string resourceName)
        {
            return GetResourceAsString(Assembly.GetCallingAssembly(), resourceName);
        }

        /// <summary>
        /// Get a string from a resource file stored in a specific assembly.
        /// </summary>
        /// <param name="assembly">Assembly which houses the resource file.</param>
        /// <param name="resourceName">Name of the resource.</param>
        public static string GetResourceAsString(Assembly assembly, string resourceName)
        {
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                if (stream != null)
                    using (StreamReader reader = new StreamReader(stream))
                        return reader.ReadToEnd();

            return null;
        }

        /// <summary>
        /// Get a string from a resource file stored in the current assembly.
        /// Returns the string as a string array where each line is an element of the array.
        /// </summary>
        /// <param name="resourceName">Name of the resource.</param>
        public static string[] GetResourceAsStringArray(string resourceName)
        {
            string fullString = GetResourceAsString(Assembly.GetCallingAssembly(), resourceName);
            return fullString.Split('\n'); ;
        }

        /// <summary>
        /// Copy the contents of a resource into a file on disk.
        /// </summary>
        /// <param name="assembly">Assembly to which the resource belongs.</param>
        /// <param name="resource">Name of the resource.</param>
        /// <param name="file">Path to the file to be written.</param>
        public static void WriteResourceToFile(Assembly assembly, string resource, string file)
        {
            using (Stream reader = assembly.GetManifestResourceStream(resource))
            {
                using (FileStream writer = File.Create(file))
                {
                    reader.Seek(0, SeekOrigin.Begin);
                    reader.CopyTo(writer);
                }
            }
        }
    }
}
