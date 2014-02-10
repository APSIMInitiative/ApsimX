using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Models.Core;

namespace Models.PMF.Functions
{
    [Serializable]
    [Description("Returns the maximum value of all childern functions")]
    public class MaximumFunction : Function
    {
        private List<Function> Children { get { return ModelsMatching<Function>(); } }

        public override double Value
        {
            get
            {
                double ReturnValue = -999999999;
                foreach (Function F in Children)
                {
                    ReturnValue = Math.Max(ReturnValue, F.Value);
                }
                return ReturnValue;
            }
        }

    }
}