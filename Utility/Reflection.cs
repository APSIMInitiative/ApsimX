using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.CodeDom.Compiler;

namespace Utility
{
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

                PropertyInfo P = Obj.GetType().GetProperty(Name, Flags);
                if (P != null)
                {
                    if (P.PropertyType == typeof(string))
                        P.SetValue(Obj, Value.ToString(), null);
                    else if (P.PropertyType == typeof(double))
                        P.SetValue(Obj, Convert.ToDouble(Value), null);
                    else if (P.PropertyType == typeof(int))
                        P.SetValue(Obj, Convert.ToInt32(Value), null);
                    else
                        P.SetValue(Obj, Value, null);
                    return true;
                }

                return false;
            }
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

        public static Assembly CompileTextToAssembly(string Code)
        {
            bool VB = Code.IndexOf("Imports System") != -1;
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
                    Params.GenerateInMemory = true;      //Assembly is created in memory
                    Params.TempFiles = new TempFileCollection(System.IO.Path.GetTempPath(), false);
                    Params.TreatWarningsAsErrors = false;
                    Params.WarningLevel = 2;
                    Params.ReferencedAssemblies.Add("System.dll");
                    Params.ReferencedAssemblies.Add("System.Xml.dll");
                    Params.ReferencedAssemblies.Add(System.IO.Path.Combine(Assembly.GetExecutingAssembly().Location));

                    Params.TempFiles = new TempFileCollection(".");
                    Params.TempFiles.KeepFiles = false;
                    string[] source = new string[1];
                    source[0] = Code;
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

                    return results.CompiledAssembly;
                }
            }
            throw new Exception("Cannot compile manager script to an assembly");
        }
    }
}
