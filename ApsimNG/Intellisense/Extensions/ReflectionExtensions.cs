using System;
using System.Reflection;

namespace UserInterface.Intellisense.Extensions
{
    internal static class ReflectionExtensions
    {
        public static T InvokeStatic<T>(this MethodInfo methodInfo, object[] args)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            return (T)methodInfo.Invoke(null, args);
        }
    }
}
