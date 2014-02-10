using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Models.Core;

namespace Models.PMF.Functions
{
    [Serializable]
    [Description("Returns the Minimum value of all children functions")]
    public class MinimumFunction : Function
    {
        private List<Function> Children { get { return ModelsMatching<Function>(); } }

        public override double Value
        {
            get
            {
                double ReturnValue = 999999999;
                foreach (Function F in Children)
                {
                    ReturnValue = Math.Min(ReturnValue, F.Value);
                }
                return ReturnValue;
            }
        }

    }
}