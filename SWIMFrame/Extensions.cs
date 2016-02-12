using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using MathNet.Numerics.LinearAlgebra;

namespace SWIMFrame
{
    /// <summary>
    /// From http://www.dotnetperls.com/array-slice. Provides a simpler way of slicing arrays
    /// when converting from FORTRAN.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Get the array slice between the two indexes.
        /// ... Inclusive for start and end indexes.
        /// </summary>
        public static T[] Slice<T>(this T[] source, int start, int end)
        {
            int len = end - start + 1;

            // Return new array.
            T[] res = new T[len+1];
            for (int i = 0; i < len; i++)
            {
                res[i + 1] = source[i + start];
            }
            return res;
        }

        /// <summary>
        /// General method to allow calling of private members for unit testing.
        /// http://www.codeproject.com/Articles/19911/Dynamically-Invoke-A-Method-Given-Strings-with-Met
        /// </summary>
        /// <param name="typeName" type="string">the class type that the method belongs to</param>
        /// <param name="methodName" type="string">name of the method</param>
        /// <param name="parameters" type="params object[]">one or more parameters for the method you wish to call</param>
        /// <returns type="object">returns an object if it has anything to return</returns>
        public static object TestMethod(string typeName, string methodName, params object[] parameters)
        {

            //get the type of the class
            Type theType = Type.GetType("SWIMFrame." + typeName + ", SWIMFrame");

            //invoke the method from the string. if there is anything to return, it returns to obj.
            object obj = theType.InvokeMember(methodName, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, null, parameters);

            //return the object if it exists
            return obj;
        }
    }
}
