using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.PMF.Functions
{
    /// <summary>FIXME: This can be generalised to a IF function</summary>
    [Serializable]
    [Description("Tests if value of the first child is less than value of second child. Returns third child if true and forth if false")]
    public class LessThanFunction : Model, IFunction
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

                double TestVariable = 0.0;
                double LessThanCriteria = 0.0;
                double IfTrue = 0.0;
                double IfFalse = 0.0;

                IFunction F = ChildFunctions[0] as IFunction;

                for (int i = 0; i < ChildFunctions.Count; i++)
                {
                    F = ChildFunctions[i] as IFunction;
                    if (i == 0)
                        TestVariable = F.Value;
                    if (i == 1)
                        LessThanCriteria = F.Value;
                    if (i == 2)
                        IfTrue = F.Value;
                    if (i == 3)
                        IfFalse = F.Value;
                }

                if (TestVariable < LessThanCriteria)
                    return IfTrue;
                else
                    return IfFalse;
            }
        }

    }
}