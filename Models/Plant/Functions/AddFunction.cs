using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.PMF.Functions
{
    [Serializable]
    [Description("Add the values of all child functions")]
    public class AddFunction : Function
    {
        private List<IModel> ChildFunctions;

        public override double Value
        {
            get
            {
                if (ChildFunctions == null)
                    ChildFunctions = Apsim.Children(this, typeof(Function)); 

                double returnValue = 0.0;

                foreach (Function F in ChildFunctions)
                {
                    returnValue = returnValue + F.Value;
                }

                return returnValue;
            }
        }

    }

}