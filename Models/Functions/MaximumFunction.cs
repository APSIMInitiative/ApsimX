using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Documentation;
using Models.Core;

namespace Models.Functions
{
    /// <summary>This class calculates the minimum of all child functions.</summary>
    [Serializable]
    [Description("Returns the maximum value of all childern functions")]
    public class MaximumFunction : Model, IFunction
    {
        /// <summary>The child functions</summary>
        private IEnumerable<IFunction> ChildFunctions;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (ChildFunctions == null)
                ChildFunctions = FindAllChildren<IFunction>().ToList();

            double ReturnValue = -999999999;
            foreach (IFunction F in ChildFunctions)
            {
                ReturnValue = Math.Max(ReturnValue, F.Value(arrayIndex));
            }
            return ReturnValue;
        }
        /// <summary>String list of child functions</summary>
        public string ChildFunctionList
        {
            get
            {
                return AutoDocumentation.ChildFunctionList(this.FindAllChildren<IFunction>().ToList());
            }
        }

        /// <summary>Document the model.</summary>
        public override IEnumerable<ITag> Document()
        {
            foreach (ITag tag in MinimumFunction.DocumentMinMaxFunction("Max", Name, Children))
                yield return tag;
        }
    }
}