using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.PMF.Functions
{
    /// <summary>
    /// Multiplies the values of the children of this node
    /// </summary>
    [Serializable]
    [Description("Returns the product of all childern function values")]
    public class MultiplyFunction : Function
    {
        private List<Function> Children { get; set; }
        public override double Value
        {
            get
            {
                if (Children == null)
                    Children = ModelsMatching<Function>();

                double returnValue = 1.0;

                foreach (Function F in Children)
                    returnValue = returnValue * F.Value;
                return returnValue;
            }
        }

    }
}