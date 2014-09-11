using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Models.Core;

namespace Models.PMF.Functions
{
    /// <summary>
    /// Maximize the values of the children of this node
    /// </summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval Maximum value of all children of this node. Return -999999999 if no child.
    /// <remarks>
    /// </remarks>
    [Serializable]
    [Description("Returns the maximum value of all childern functions")]
    public class MaximumFunction : Function
    {
        private List<IModel> ChildFunctions;

        public override double Value
        {
            get
            {
                if (ChildFunctions == null)
                    ChildFunctions = Apsim.Children(this, typeof(Function));

                double ReturnValue = -999999999;
                foreach (Function F in ChildFunctions)
                {
                    ReturnValue = Math.Max(ReturnValue, F.Value);
                }
                return ReturnValue;
            }
        }

    }
}