// -----------------------------------------------------------------------
// <copyright file="Reflection.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Utility
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;

    public class Reflection
    {
        /// <summary>
        /// Returns true if the specified type T is of type TypeName
        /// <summary>
        public static bool IsOfType(Type T, string TypeName)
        {
            while (T != null)
            {
                if (T.ToString() == TypeName)
                    return true;

                if (T.GetInterface(TypeName) != null)
                    return true;

                T = T.BaseType;
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
        /// Get the value of a field or property.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Obj"></param>
        /// <returns></returns>
        public static object GetValueOfFieldOrProperty(string Name, object Obj)
        {
            int Pos = Name.IndexOf('.');
            if (Pos > -1)
            {
                string FieldName = Name.Substring(0, Pos);
                Obj = GetValueOfFieldOrProperty(FieldName, Obj);
                if (Obj == null)
                    return null;
                else
                    return GetValueOfFieldOrProperty(Name.Substring(Pos + 1), Obj);
            }
            else
            {
                BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
                FieldInfo F = Obj.GetType().GetField(Name, Flags);
                if (F != null)
                    return F.GetValue(Obj);

                PropertyInfo P = Obj.GetType().GetProperty(Name, Flags);
                if (P != null)
                    return P.GetValue(Obj, null);

                return null;
            }
        }

        /// <summary>
        /// Trys to set the value of a public or private field or property. Name can have '.' characters. Will
        /// return true if successfull. Will throw if Value is the wrong type for the field
        /// or property. Supports strings/double/int conversion or direct setting.
        /// </summary>
        public static bool SetValueOfFieldOrProperty(string Name, object Obj, object Value)
        {
            if (Name.Contains("."))
            {
                int Pos = Name.IndexOf('.');
                string FieldName = Name.Substring(0, Pos);
                Obj = SetValueOfFieldOrProperty(FieldName, Obj, Value);
                if (Obj == null)
                    return false;
                else
                    return SetValueOfFieldOrProperty(Name.Substring(Pos + 1), Obj, Value);
            }
            else
            {
                BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
                FieldInfo F = Obj.GetType().GetField(Name, Flags);
                if (F != null)
                {
                    if (F.FieldType == typeof(string))
                        F.SetValue(Obj, Value.ToString());
                    else if (F.FieldType == typeof(double))
                        F.SetValue(Obj, Convert.ToDouble(Value));
                    else if (F.FieldType == typeof(int))
                        F.SetValue(Obj, Convert.ToInt32(Value));
                    else
                        F.SetValue(Obj, Value);
                    return true;
                }

                return SetValueOfProperty(Name, Obj, Value);
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
                    P.SetValue(obj, Convert.ToDouble(value), null);
                else if (P.PropertyType == typeof(int))
                    P.SetValue(obj, Convert.ToInt32(value), null);
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
        public static Type[] GetTypeWithoutNameSpace(string className)
        {
            List<Type> returnVal = new List<Type>();

            Type[] assemblyTypes = Assembly.GetExecutingAssembly().GetTypes();
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
        public static Attribute GetAttribute(Type T, Type AttributeTypeToFind, bool LookInBaseClasses)
        {
            foreach (Attribute A in T.GetCustomAttributes(LookInBaseClasses))
            {
                if (A.GetType() == AttributeTypeToFind)
                    return A;
            }
            return null;
        }

        /// <summary>
        /// Gets the specified attribute type.
        /// </summary>
        /// <returns>Returns the attribute or null if not found.</returns>
        public static Attribute GetAttribute(MemberInfo T, Type AttributeTypeToFind, bool LookInBaseClasses)
        {
            foreach (Attribute A in T.GetCustomAttributes(LookInBaseClasses))
            {
                if (A.GetType() == AttributeTypeToFind)
                    return A;
            }
            return null;
        }

        /// <summary>
        /// Gets 0 or more attributes of the specified type.
        /// </summary>
        /// <returns>Returns the attributes or string[0] if none found.</returns>
        public static Attribute[] GetAttributes(Type T, Type AttributeTypeToFind, bool LookInBaseClasses)
        {
            List<Attribute> Attributes = new List<Attribute>();
            foreach (Attribute A in T.GetCustomAttributes(LookInBaseClasses))
            {
                if (A.GetType() == AttributeTypeToFind)
                    Attributes.Add(A);
            }
            return Attributes.ToArray();
        }

        /// <summary>
        /// Gets 0 or more attributes of the specified type.
        /// </summary>
        /// <returns>Returns the attributes or string[0] if none found.</returns>
        public static Attribute[] GetAttributes(MemberInfo T, Type AttributeTypeToFind, bool LookInBaseClasses)
        {
            List<Attribute> Attributes = new List<Attribute>();
            foreach (Attribute A in T.GetCustomAttributes(LookInBaseClasses))
            {
                if (A.GetType() == AttributeTypeToFind)
                    Attributes.Add(A);
            }
            return Attributes.ToArray();
        }

        /// <summary>
        /// Returns the name of the specified object if it has a public name property
        /// or it returns the name of the type if no name property is present.
        /// </summary>
        public static string Name(object Obj)
        {
            if (Obj != null)
            {
                PropertyInfo NameProperty = Obj.GetType().GetProperty("Name");
                if (NameProperty == null)
                    return Obj.GetType().Name;
                else
                    return NameProperty.GetValue(Obj, null) as string;
            }
            return null;
        }

        /// <summary>
        /// Sets the name of the specified object if it has a public name property that is settable.
        /// Will throw if cannot set the name.
        /// </summary>
        public static void SetName(object Obj, string NewName)
        {
            PropertyInfo NameProperty = Obj.GetType().GetProperty("Name");
            if (NameProperty == null || !NameProperty.CanWrite)
                throw new Exception("Cannot set the name of object with type: " + Obj.GetType().Name + 
                                    ". It does not have a public, settable, name property");
            else
                NameProperty.SetValue(Obj, NewName, null);
        }

        /// <summary>
        /// Returns true if the specified object has a name property with a public setter.
        /// </summary>
        public static bool NameIsSettable(object Obj)
        {
            PropertyInfo NameProperty = Obj.GetType().GetProperty("Name");
            return NameProperty != null && NameProperty.CanWrite;
        }

        /// <summary>
        /// Return a type from the specified unqualified (no namespaces) type name.
        /// </summary>
        public static Type GetTypeFromUnqualifiedName(string typeName)
        {
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.Name == typeName)
                    return type;
            }
            return null;
        }

        /// <summary>
        /// An assembly cache.
        /// </summary>
        private static Dictionary<string, Assembly> AssemblyCache = new Dictionary<string, Assembly>();

        /// <summary>
        /// Compile the specified 'code' into an executable assembly. If 'assemblyFileName'
        /// is null then compile to an in-memory assembly.
        /// </summary>
        public static Assembly CompileTextToAssembly(string code, string assemblyFileName)
        {
            // See if we've already compiled this code. If so then return the assembly.
            if (AssemblyCache.ContainsKey(code))
                return AssemblyCache[code];

            lock (AssemblyCache)
            {
                if (AssemblyCache.ContainsKey(code))
                    return AssemblyCache[code];
                bool VB = code.IndexOf("Imports System") != -1;
                string Language;
                if (VB)
                    Language = CodeDomProvider.GetLanguageFromExtension(".vb");
                else
                    Language = CodeDomProvider.GetLanguageFromExtension(".cs");

                if (Language != null && CodeDomProvider.IsDefinedLanguage(Language))
                {
                    CodeDomProvider Provider = CodeDomProvider.CreateProvider(Language);
                    if (Provider != null)
                    {
                        CompilerParameters Params = new CompilerParameters();

                        if (assemblyFileName == null)
                            Params.GenerateInMemory = true;
                        else
                        {
                            Params.GenerateInMemory = false;
                            Params.OutputAssembly = assemblyFileName;
                        }
                        Params.TreatWarningsAsErrors = false;
                        Params.WarningLevel = 2;
                        Params.ReferencedAssemblies.Add("System.dll");
                        Params.ReferencedAssemblies.Add("System.Xml.dll");
                        Params.ReferencedAssemblies.Add(System.IO.Path.Combine(Assembly.GetExecutingAssembly().Location));
                        if (Assembly.GetCallingAssembly() != Assembly.GetExecutingAssembly())
                            Params.ReferencedAssemblies.Add(System.IO.Path.Combine(Assembly.GetCallingAssembly().Location));
                        Params.TempFiles = new TempFileCollection(".");
                        Params.TempFiles.KeepFiles = false;
                        string[] source = new string[1];
                        source[0] = code;
                        CompilerResults results = Provider.CompileAssemblyFromSource(Params, source);
                        string Errors = "";
                        foreach (CompilerError err in results.Errors)
                        {
                            if (Errors != "")
                                Errors += "\r\n";

                            Errors += err.ErrorText + ". Line number: " + err.Line.ToString();
                        }
                        if (Errors != "")
                            throw new Exception(Errors);

                        AssemblyCache.Add(code, results.CompiledAssembly);
                        return results.CompiledAssembly;
                    }
                }
                throw new Exception("Cannot compile manager script to an assembly");
            }
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
        /// Convert the specified 'stringValue' into an object of the specified 'type'.
        /// Will throw if cannot convert type.
        /// </summary>
        public static object StringToObject(Type type, string stringValue)
        {
            if (type.IsArray)
            {
                string[] stringValues = stringValue.ToString().Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (type == typeof(double[]))
                    return Utility.Math.StringsToDoubles(stringValues);
                else if (type == typeof(int[]))
                    return Utility.Math.StringsToDoubles(stringValues);
                else if (type == typeof(string[]))
                    return stringValues;
                else
                    throw new Exception("Cannot convert '" + stringValue + "' into an object of type '" + type.ToString() + "'");
            }
            else if (type == typeof(double))
                return Convert.ToDouble(stringValue);
            else if (type == typeof(float))
                return Convert.ToSingle(stringValue);
            else if (type == typeof(int))
                return Convert.ToInt32(stringValue);
            else if (type == typeof(DateTime))
                return Convert.ToDateTime(stringValue);
            else if (type == typeof(string))
                return stringValue;
            else if (type == typeof(bool))
                return Boolean.Parse(stringValue);
            else if (type.IsEnum)
                return Enum.Parse(type, stringValue, true);

            throw new Exception("Cannot convert the string '" + stringValue + "' to a " + type.ToString());
        }

        /// <summary>
        /// Convert the specified 'obj' into a string.
        /// </summary>
        public static string ObjectToString(object obj)
        {
            if (obj.GetType().IsArray)
            {
                string stringValue = "";
                Array arr = obj as Array;
                for (int j = 0; j < arr.Length; j++)
                {
                    if (j > 0)
                        stringValue += ",";
                    stringValue += arr.GetValue(j).ToString();
                }
                return stringValue;
            }
            else if (obj.GetType() == typeof(DateTime))
            {
                return ((DateTime) obj).ToString("yyyy-MM-dd");
            }
            else
            {
                return obj.ToString();
            }
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
    }
}
