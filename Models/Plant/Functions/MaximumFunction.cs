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
        private Model[] ChildFunctions;

        public override double Value
        {
            get
            {
                if (ChildFunctions == null)
                    ChildFunctions = Children.MatchingMultiple(typeof(Function));

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