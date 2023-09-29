﻿using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Documentation;
using Models.Core;

namespace Models.Functions
{
    /// <summary>A class that returns the sum of its child functions.</summary>

    [Serializable]
    [Description("Add the values of all child functions")]
    public class AddFunction : Model, IFunction
    {
        /// <summary>The child functions</summary>
        private IEnumerable<IFunction> ChildFunctions;

        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            if (ChildFunctions == null)
                ChildFunctions = FindAllChildren<IFunction>().ToList();

            double returnValue = 0.0;

            foreach (IFunction F in ChildFunctions)
                returnValue = returnValue + F.Value(arrayIndex);

            return returnValue;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        public override IEnumerable<ITag> Document()
        {
            foreach (var tag in MultiplyFunction.DocumentMathFunction('+', Name, Children))
                yield return tag;
        }
    }

}