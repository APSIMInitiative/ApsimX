using System;
using System.Collections.Generic;
using System.Linq;

namespace APSIM.Shared.Extensions
{

    /// <summary>
    /// Extensions for type Type.
    /// </summary>
    public static class TypeExtensions
    {
        private static readonly Dictionary<Type, string> aliases = new Dictionary<Type, string>()
                                                                    {
                                                                        { typeof(byte), "byte" },
                                                                        { typeof(sbyte), "sbyte" },
                                                                        { typeof(short), "short" },
                                                                        { typeof(ushort), "ushort" },
                                                                        { typeof(int), "int" },
                                                                        { typeof(uint), "uint" },
                                                                        { typeof(long), "long" },
                                                                        { typeof(ulong), "ulong" },
                                                                        { typeof(float), "float" },
                                                                        { typeof(double), "double" },
                                                                        { typeof(decimal), "decimal" },
                                                                        { typeof(object), "object" },
                                                                        { typeof(bool), "bool" },
                                                                        { typeof(char), "char" },
                                                                        { typeof(string), "string" },
                                                                        { typeof(void), "void" }
                                                                    };

        /// <summary>
        /// Get a C# readable type name for a type. This will return the name of the
        /// type, or, if the type is a generic type, the C#-readable name of the type,
        /// with the generic type arguments in angled brackets.
        /// </summary>
        /// <param name="type">The type.</param>
        public static string GetFriendlyName(this Type type)
        {
            string friendlyName = type.Name;
            if (aliases.ContainsKey(type))
                friendlyName = aliases[type];
            if (type.IsGenericType)
            {
                int backtick = friendlyName.IndexOf('`');
                if (backtick > 0)
                    friendlyName = friendlyName.Remove(backtick);
                string typeParameters = string.Join(", ", type.GetGenericArguments().Select(a => a.GetFriendlyName()));
                friendlyName += $"<{typeParameters}>";
            }

            return friendlyName;
        }
    }
}