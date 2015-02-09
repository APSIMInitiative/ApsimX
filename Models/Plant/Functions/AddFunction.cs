using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.PMF.Functions
{
    /// <summary>
    /// A function that adds values from child functions
    /// </summary>
    [Serializable]
    [Description("Add the values of all child functions")]
    public class AddFunction : Model, IFunction
    {
        /// <summary>The child functions</summary>
        private List<IModel> ChildFunctions;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value
        {
            get
            {
                if (ChildFunctions == null)
                    ChildFunctions = Apsim.Children(this, typeof(IFunction)); 

                double returnValue = 0.0;

                foreach (IFunction F in ChildFunctions)
                {
                    returnValue = returnValue + F.Value;
                }

                return returnValue;
            }
        }

    }

}