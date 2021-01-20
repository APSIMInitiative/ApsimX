using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;
using System.IO;
using System.Linq;

namespace Models.Functions
{
    /// <summary>
    /// [DocumentMathFunction x]
    /// </summary>
    [Serializable]
    [Description("Returns the product of all children function values")]
    public class MultiplyFunction : Model, IFunction//, ICustomDocumentation
    {
        /// <summary>The child functions</summary>
        private IEnumerable<IFunction> ChildFunctions;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (ChildFunctions == null)
                ChildFunctions = FindAllChildren<IFunction>().ToList();

            double returnValue = 1.0;

            foreach (IFunction F in ChildFunctions)
                returnValue = returnValue * F.Value(arrayIndex);
            return returnValue;
        }
    }
}