namespace APSIM.Shared.Utilities
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;

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
            if (type == typeof(Object)) return new List<FieldInfo>();

            var list = GetAllFields(type.BaseType, flags);
            // in order to avoid duplicates, force BindingFlags.DeclaredOnly
            list.AddRange(type.GetFields(flags | BindingFlags.DeclaredOnly));
            return list;
        }

        /// <summary>
        /// Return all properties. The normal .NET reflection doesn't return private fields in base classes.
        /// This function does.
        /// </summary>
        public static List<PropertyInfo> GetAllProperties(Type type, BindingFlags flags)
        {
            if (type == typeof(Object)) return new List<PropertyInfo>();

            var list = GetAllProperties(type.BaseType, flags);
            // in order to avoid duplicates, force BindingFlags.DeclaredOnly
            list.AddRange(type.GetProperties(flags | BindingFlags.DeclaredOnly));
            return list;
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
                BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
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
        /// Binary serialise the object and return the resulting stream.
        /// </summary>
        public static Stream BinarySerialise(object source)
        {
            if (source == null)
                return null;

            if (!source.GetType().IsSerializable)
                throw new ArgumentException("The type must be serializable.", "source");

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            formatter.Serialize(stream, source);
            return stream;
        }

        /// <summary>
        /// Binary deserialise the specified stream and return the resulting object
        /// </summary>
        public static object BinaryDeserialise(Stream stream)
        {
            if (stream == null)
                return null;

            IFormatter formatter = new BinaryFormatter();
            return formatter.Deserialize(stream);
        }

        /// <summary>
        /// Convert an object into a json string. 
        /// </summary>
        /// <param name="source">The source object.</param>
        /// <param name="includePrivates">Serialise private members as well as publics?</param>
        /// <returns>The string representation of the object.</returns>
        public static string JsonSerialise(object source, bool includePrivates)
        {
            return JsonConvert.SerializeObject(source, Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new DynamicContractResolver(includePrivates),
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });
        }

        ///<summary> Custom Contract resolver to stop deseralization of Parent properties </summary>
        private class DynamicContractResolver : DefaultContractResolver
        {
            private BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

            public DynamicContractResolver(bool includePrivates)
            {
                if (includePrivates)
                    bindingFlags |= BindingFlags.NonPublic;
            }

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var props = GetAllFields(type, bindingFlags).Select(p => base.CreateProperty(p, memberSerialization))
                            .Union(
                            GetAllProperties(type, bindingFlags).Select(p => base.CreateProperty(p, memberSerialization))
                            ).ToList();
                props.ForEach(p => { p.Writable = true; p.Readable = true; });
                return props.Where(p => p.PropertyName != "Parent").ToList();
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

            if (dataType.IsArray)
            {
                // Arrays do not implement IConvertible, so we cannot just split the string on
                // the commas and parse the string array into Convert.ChangeType. Instead, we
                // must convert each element of the array individually.
                object[] arr = newValue.Split(',').Select(s => StringToObject(dataType.GetElementType(), s, format)).ToArray();

                // An object array is not good enough. We need an array with correct element type.
                Array result = Array.CreateInstance(dataType.GetElementType(), arr.Length);
                Array.Copy(arr, result, arr.Length);
                return result;
            }

            // Do we really want enums to be case-insensitive?
            if (dataType.IsEnum)
                return Enum.Parse(dataType, newValue, true);

            // Convert.ChangeType() doesn't seem to work properly on nullable types.
            Type underlyingType = Nullable.GetUnderlyingType(dataType);
            if (underlyingType != null)
                dataType = underlyingType;

            return Convert.ChangeType(newValue, dataType, format);
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
            if (obj.GetType().IsArray)
            {
                string stringValue = "";
                Array arr = obj as Array;
                for (int j = 0; j < arr.Length; j++)
                {
                    if (j > 0)
                        stringValue += ",";
                    stringValue += ObjectToString(arr.GetValue(j));
                }
                return stringValue;
            }
            else
                return Convert.ToString(obj, format);
        }

        /// <summary>
        /// Perform a deep Copy of the specified object
        /// </summary>
        public static object Clone(object sourceObj)
        {
            Stream stream = BinarySerialise(sourceObj);
            stream.Seek(0, SeekOrigin.Begin);
            return BinaryDeserialise(stream);
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
            {
                foreach (Type t in assembly.GetTypes())
                {
                    if (interfaceType.IsAssignableFrom(t) && t.Name != interfaceType.Name && t.IsPublic)
                        types.Add(t);
                }
            }

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
    }
}
