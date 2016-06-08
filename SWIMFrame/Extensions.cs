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
        static StringBuilder sb = new StringBuilder();
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

        /// <summary>
        /// Populate an array with a value.
        /// http://stackoverflow.com/questions/1014005/how-to-populate-instantiate-a-c-sharp-array-with-a-single-value
        /// </summary>
        /// <typeparam name="T">the class type that the method belongs to</typeparam>
        /// <param name="arr">The array to fill.</param>
        /// <param name="value">The value to fill with.</param>
        public static T[] Populate<T>(this T[] arr, T value)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = value;
            }
            return arr;
        }

        /// <summary>
        /// Populate a 2D array.
        /// </summary>
        /// <typeparam name="T">the class type that the method belongs to</typeparam>
        /// <param name="arr">The 2D array to populate.</param>
        /// <param name="value">The value to fill with.</param>
        /// <returns></returns>
        public static T[,] Populate2D<T>(this T[,] arr, T value)
        {
            for (int x = 0; x > arr.GetLength(0); x++)
                for (int y = 0; y < arr.GetLength(1); y++)
                {
                arr[x,y] = value;
            }
            return arr;
        }

        /// <summary>
        /// Returns a row or column from the given 2D array.
        /// </summary>
        /// <param name="m">The array.</param>
        /// <param name="index">The index to return.</param>
        /// <param name="row">True for row, false for column</param>
        /// <returns></returns>
        public static double[] GetRowCol(double[,] m, int index, bool column)
        {
            double[] retVal;
            if(column)
            {
                retVal = new double[m.GetLength(0)];
                for (int i = 0; i < m.GetLength(0); i++)
                    retVal[i] = m[i, index];
            }
            else
            {
                retVal = new double[m.GetLength(1)];
                for (int i = 0; i < m.GetLength(1); i++)
                    retVal[i] = m[index,i];
            }
            return retVal;
        }

        /// <summary>
        /// Write an object to a log file with formatting.
        /// </summary>
        /// <param name="method">The name of the calling method.</param>
        /// <param name="type">Type representation.</param>
        /// <param name="obj">The object to write.</param>
        public static void Log(string method, string type, object obj)
        {
            switch (type)
            {
                case "i": //int
                case "d": //double
                case "s": //string
                    sb.AppendLine(method + " " + obj.ToString());
                    break;
                case "i1": //int 1D
                    int[] i1 = obj as int[];
                    sb.Append(method + " ");
                    foreach (int i in i1.Skip(1))
                        sb.Append(i + " ");
                    sb.AppendLine();
                    break;
                case "d1": //double 1D
                    double[] d1 = obj as double[];
                    sb.Append(method + " ");
                    foreach (double d in d1.Skip(1))
                        sb.Append(d + " ");
                    sb.AppendLine();
                    break;
                case "i2": //int 2D
                    int[,] i2 = obj as int[,];
                    for (int i = 1; i < i2.GetLength(0); i++)
                    {
                        sb.Append(method + " " + i + " ");
                        for (int j = 1; j < i2.GetLength(0); j++)
                            sb.Append(i2[i, j] + " ");
                        sb.AppendLine();
                    }
                    break;
                case "d2": //double 2D
                    double[,] d2 = obj as double[,];
                    for (int i = 1; i < d2.GetLength(0); i++)
                    {
                        sb.Append(method + " " + i + " ");
                        for (int j = 1; j < d2.GetLength(0); j++)
                            sb.Append(d2[i, j] + " ");
                        sb.AppendLine();
                    }
                    break;
            }
        }

        public static void WriteLog()
        {
            System.IO.File.WriteAllText(@"C:\Users\fai04d\OneDrive\SWIM Conversion 2015\log.txt", sb.ToString());
        }
    }
}
